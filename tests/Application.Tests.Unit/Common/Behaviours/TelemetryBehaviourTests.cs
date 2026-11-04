namespace CleanWebApi.Application.Tests.Unit.Common.Behaviours;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Application.Authors.Entities;
using Application.Authors.Queries.GetAuthorById;
using Application.Common.Behaviours;
using Application.Common.Enums;
using Application.Common.Exceptions;
using Application.Common.Telemetry;
using FluentValidation;
using FluentValidation.Results;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

public sealed class TelemetryBehaviourTests : IDisposable
{
    private const string TestSourceName = "CleanWebApi.Application.Tests";

    private static readonly ActivitySource TestSource = new(TestSourceName);

    private readonly ServiceProvider services = new ServiceCollection().AddMetrics().BuildServiceProvider();
    private readonly ActivityListener listener;
    private readonly List<Activity> spans = [];
    private readonly FakeTimeProvider timeProvider = new();
    private readonly MetricCollector<long> requests;
    private readonly MetricCollector<double> duration;
    private readonly TelemetryBehaviour<GetAuthorByIdQuery, Author> behaviour;
    private readonly GetAuthorByIdQuery query = new() { Id = Guid.NewGuid() };
    private readonly MessageHandlerDelegate<GetAuthorByIdQuery, Author> next = Substitute.For<MessageHandlerDelegate<GetAuthorByIdQuery, Author>>();

    public TelemetryBehaviourTests()
    {
        // Other tests run at the same time, so only spans from this test's own trace are kept.
        this.listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is ApplicationTelemetry.Name or TestSourceName,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (this.spans)
                {
                    this.spans.Add(activity);
                }
            }
        };

        ActivitySource.AddActivityListener(this.listener);

        var meterFactory = this.services.GetRequiredService<IMeterFactory>();

        this.requests = new MetricCollector<long>(meterFactory, ApplicationTelemetry.Name, "application.requests");
        this.duration = new MetricCollector<double>(meterFactory, ApplicationTelemetry.Name, "application.request.duration");
        this.behaviour = new TelemetryBehaviour<GetAuthorByIdQuery, Author>(new ApplicationTelemetry(meterFactory), this.timeProvider);
    }

    [Fact]
    public async Task Handle_ShouldRecordHandled_WhenNextSucceeds()
    {
        // Arrange
        var author = new Author(Guid.NewGuid(), "Test", "Test", []);

        _ = this.next(default!, default).ReturnsForAnyArgs(_ =>
        {
            this.timeProvider.Advance(TimeSpan.FromMilliseconds(12.5));
            return author;
        });

        using var parent = TestSource.StartActivity("Test")!;

        // Act
        var result = await this.behaviour.Handle(this.query, this.next, CancellationToken.None);

        // Assert
        result.ShouldBe(author);

        var span = this.SpanIn(parent);
        span.DisplayName.ShouldBe("GetAuthorByIdQuery");
        span.ParentSpanId.ShouldBe(parent.SpanId);
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem(ApplicationTelemetry.TagNames.RequestName).ShouldBe("GetAuthorByIdQuery");
        span.GetTagItem(ApplicationTelemetry.TagNames.RequestOutcome).ShouldBe(ApplicationTelemetry.RequestOutcome.Handled);

        var count = this.requests.LastMeasurement!;
        count.Value.ShouldBe(1);
        count.Tags[ApplicationTelemetry.TagNames.RequestName].ShouldBe("GetAuthorByIdQuery");
        count.Tags[ApplicationTelemetry.TagNames.RequestOutcome].ShouldBe(ApplicationTelemetry.RequestOutcome.Handled);
        this.duration.LastMeasurement!.Value.ShouldBe(0.0125, 0.000001);
    }

    [Fact]
    public async Task Handle_ShouldCopyBaggage_ToSpanTags()
    {
        // Arrange
        _ = this.next(default!, default).ReturnsForAnyArgs(new Author(Guid.NewGuid(), "Test", "Test", []));

        using var parent = TestSource.StartActivity("Test")!;
        _ = parent.AddBaggage("tenant.id", "acme");

        // Act
        _ = await this.behaviour.Handle(this.query, this.next, CancellationToken.None);

        // Assert
        this.SpanIn(parent).GetTagItem("tenant.id").ShouldBe("acme");
    }

    [Fact]
    public async Task Handle_ShouldRecordInvalid_WhenValidationFails()
    {
        // Arrange
        _ = this.next(default!, default).ThrowsAsyncForAnyArgs(new ValidationException(
        [
            new ValidationFailure("Id", "'Id' must not be empty."),
            new ValidationFailure("Id", "'Id' must be a real id."),
            new ValidationFailure("Name", "'Name' must not be empty.")
        ]));

        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = await Should.ThrowAsync<ValidationException>(async () => await this.behaviour.Handle(this.query, this.next, CancellationToken.None));

        // Assert
        var span = this.SpanIn(parent);
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem(ApplicationTelemetry.TagNames.RequestOutcome).ShouldBe(ApplicationTelemetry.RequestOutcome.Invalid);

        var spanEvent = span.Events.ShouldHaveSingleItem();
        spanEvent.Name.ShouldBe("validation_failed");
        spanEvent.Tags.ShouldContain(new KeyValuePair<string, object?>(ApplicationTelemetry.TagNames.ValidationProperties, "Id, Name"));

        this.requests.LastMeasurement!.Tags[ApplicationTelemetry.TagNames.RequestOutcome].ShouldBe(ApplicationTelemetry.RequestOutcome.Invalid);
    }

    [Fact]
    public async Task Handle_ShouldRecordNotFound_WhenEntityIsMissing()
    {
        // Arrange
        _ = this.next(default!, default).ThrowsAsyncForAnyArgs(new NotFoundException($"The {EntityType.Author} with the supplied id was not found."));

        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = await Should.ThrowAsync<NotFoundException>(async () => await this.behaviour.Handle(this.query, this.next, CancellationToken.None));

        // Assert
        var span = this.SpanIn(parent);
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem(ApplicationTelemetry.TagNames.RequestOutcome).ShouldBe(ApplicationTelemetry.RequestOutcome.NotFound);

        var spanEvent = span.Events.ShouldHaveSingleItem();
        spanEvent.Name.ShouldBe("not_found");
        spanEvent.Tags.ShouldContain(new KeyValuePair<string, object?>("exception.message", "The Author with the supplied id was not found."));

        this.requests.LastMeasurement!.Tags[ApplicationTelemetry.TagNames.RequestOutcome].ShouldBe(ApplicationTelemetry.RequestOutcome.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldRecordCancelled_WhenCallerCancels()
    {
        // Arrange
        using var tokenSource = new CancellationTokenSource();
        await tokenSource.CancelAsync();

        _ = this.next(default!, default).ThrowsAsyncForAnyArgs(new OperationCanceledException(tokenSource.Token));

        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = await Should.ThrowAsync<OperationCanceledException>(async () => await this.behaviour.Handle(this.query, this.next, tokenSource.Token));

        // Assert
        var span = this.SpanIn(parent);
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.Events.ShouldBeEmpty();
        span.GetTagItem(ApplicationTelemetry.TagNames.RequestOutcome).ShouldBe(ApplicationTelemetry.RequestOutcome.Cancelled);
    }

    [Fact]
    public async Task Handle_ShouldRecordError_WhenExceptionIsUnexpected()
    {
        // Arrange
        _ = this.next(default!, default).ThrowsAsyncForAnyArgs(new InvalidOperationException("Broken"));

        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = await Should.ThrowAsync<InvalidOperationException>(async () => await this.behaviour.Handle(this.query, this.next, CancellationToken.None));

        // Assert
        var span = this.SpanIn(parent);
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.StatusDescription.ShouldBe("Broken");
        span.GetTagItem(ApplicationTelemetry.TagNames.RequestOutcome).ShouldBe(ApplicationTelemetry.RequestOutcome.Error);

        var spanEvent = span.Events.ShouldHaveSingleItem();
        spanEvent.Name.ShouldBe("exception");
        spanEvent.Tags.ShouldContain(new KeyValuePair<string, object?>("exception.type", typeof(InvalidOperationException).FullName));

        this.requests.LastMeasurement!.Tags[ApplicationTelemetry.TagNames.RequestOutcome].ShouldBe(ApplicationTelemetry.RequestOutcome.Error);
    }

    public void Dispose()
    {
        this.listener.Dispose();
        this.requests.Dispose();
        this.duration.Dispose();
        this.services.Dispose();
    }

    private Activity SpanIn(Activity parent)
    {
        lock (this.spans)
        {
            return this.spans.Single(s => s.TraceId == parent.TraceId && s.Source.Name == ApplicationTelemetry.Name);
        }
    }
}
