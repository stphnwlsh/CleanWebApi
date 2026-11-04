namespace CleanWebApi.Application.Tests.Unit.Common.Behaviours;

using System.Diagnostics;
using Application.Authors.Entities;
using Application.Authors.Queries.GetAuthorById;
using Application.Common.Behaviours;
using Application.Common.Enums;
using Application.Common.Exceptions;
using Application.Common.Telemetry;
using FluentValidation;
using FluentValidation.Results;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

public sealed class LoggingBehaviourTests : IDisposable
{
    private const string TestSourceName = "CleanWebApi.Application.Tests";

    private static readonly ActivitySource TestSource = new(TestSourceName);

    private readonly ActivityListener listener;
    private readonly List<Activity> spans = [];
    private readonly FakeLogger<LoggingBehaviour<GetAuthorByIdQuery, Author>> logger = new();
    private readonly FakeTimeProvider timeProvider = new();
    private readonly LoggingBehaviour<GetAuthorByIdQuery, Author> behaviour;
    private readonly GetAuthorByIdQuery query = new() { Id = Guid.NewGuid() };

    public LoggingBehaviourTests()
    {
        this.behaviour = new LoggingBehaviour<GetAuthorByIdQuery, Author>(this.logger, this.timeProvider);

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
    }

    [Fact]
    public async Task Handle_ShouldLogHandled_WhenNextSucceeds()
    {
        // Arrange
        var author = new Author(Guid.NewGuid(), "Test", "Test", []);
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;
        var next = Substitute.For<MessageHandlerDelegate<GetAuthorByIdQuery, Author>>();

        _ = next(this.query, token).Returns(_ =>
        {
            this.timeProvider.Advance(TimeSpan.FromMilliseconds(12.5));
            return author;
        });

        using var parent = TestSource.StartActivity("Test")!;

        // Act
        var result = await this.behaviour.Handle(this.query, next, token);

        // Assert
        result.ShouldBe(author);
        _ = await next.Received(1)(this.query, token);

        var record = this.logger.Collector.LatestRecord;

        this.logger.Collector.Count.ShouldBe(1);
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldBe("GetAuthorByIdQuery handled in 12.5 ms");
        record.GetStructuredStateValue("RequestName").ShouldBe("GetAuthorByIdQuery");

        var span = this.SpanIn(parent);
        span.DisplayName.ShouldBe("GetAuthorByIdQuery");
        span.ParentSpanId.ShouldBe(parent.SpanId);
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem(ApplicationTelemetry.RequestNameTag).ShouldBe("GetAuthorByIdQuery");
        span.GetTagItem(ApplicationTelemetry.RequestOutcomeTag).ShouldBe("handled");
    }

    [Fact]
    public async Task Handle_ShouldLogInvalid_WhenValidationFails()
    {
        // Arrange
        var next = Substitute.For<MessageHandlerDelegate<GetAuthorByIdQuery, Author>>();

        _ = next(default!, default).ThrowsAsyncForAnyArgs(new ValidationException(
        [
            new ValidationFailure("Id", "'Id' must not be empty."),
            new ValidationFailure("Id", "'Id' must be a real id."),
            new ValidationFailure("Name", "'Name' must not be empty.")
        ]));

        using var parent = TestSource.StartActivity("Test")!;

        // Act
        var exception = await Should.ThrowAsync<ValidationException>(async () => await this.behaviour.Handle(this.query, next, CancellationToken.None));

        // Assert
        exception.Errors.ShouldNotBeEmpty();

        var record = this.logger.Collector.LatestRecord;

        this.logger.Collector.Count.ShouldBe(1);
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldBe("GetAuthorByIdQuery failed validation in 0.0 ms for Id, Name");

        var span = this.SpanIn(parent);
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem(ApplicationTelemetry.RequestOutcomeTag).ShouldBe("invalid");
    }

    [Fact]
    public async Task Handle_ShouldLogNotFound_WhenEntityIsMissing()
    {
        // Arrange
        var next = Substitute.For<MessageHandlerDelegate<GetAuthorByIdQuery, Author>>();

        _ = next(default!, default).ThrowsAsyncForAnyArgs(_ =>
        {
            this.timeProvider.Advance(TimeSpan.FromMilliseconds(3));
            return new NotFoundException($"The {EntityType.Author} with the supplied id was not found.");
        });

        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = await Should.ThrowAsync<NotFoundException>(async () => await this.behaviour.Handle(this.query, next, CancellationToken.None));

        // Assert
        var record = this.logger.Collector.LatestRecord;

        this.logger.Collector.Count.ShouldBe(1);
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldBe("GetAuthorByIdQuery found nothing in 3.0 ms: The Author with the supplied id was not found.");

        var span = this.SpanIn(parent);
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem(ApplicationTelemetry.RequestOutcomeTag).ShouldBe("not_found");
    }

    [Fact]
    public async Task Handle_ShouldNotLog_WhenExceptionIsUnexpected()
    {
        // Arrange
        var next = Substitute.For<MessageHandlerDelegate<GetAuthorByIdQuery, Author>>();

        _ = next(default!, default).ThrowsAsyncForAnyArgs(new InvalidOperationException("Broken"));

        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = await Should.ThrowAsync<InvalidOperationException>(async () => await this.behaviour.Handle(this.query, next, CancellationToken.None));

        // Assert
        this.logger.Collector.Count.ShouldBe(0);

        var span = this.SpanIn(parent);
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.StatusDescription.ShouldBe("Broken");
        span.GetTagItem(ApplicationTelemetry.RequestOutcomeTag).ShouldBe("error");
        span.Events.ShouldHaveSingleItem().Name.ShouldBe("exception");
    }

    [Fact]
    public async Task Handle_ShouldNotLog_WhenCallerCancels()
    {
        // Arrange
        using var tokenSource = new CancellationTokenSource();
        await tokenSource.CancelAsync();

        var next = Substitute.For<MessageHandlerDelegate<GetAuthorByIdQuery, Author>>();

        _ = next(default!, default).ThrowsAsyncForAnyArgs(new OperationCanceledException(tokenSource.Token));

        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = await Should.ThrowAsync<OperationCanceledException>(async () => await this.behaviour.Handle(this.query, next, tokenSource.Token));

        // Assert
        this.logger.Collector.Count.ShouldBe(0);

        var span = this.SpanIn(parent);
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.Events.ShouldBeEmpty();
        span.GetTagItem(ApplicationTelemetry.RequestOutcomeTag).ShouldBe("cancelled");
    }

    public void Dispose()
    {
        this.listener.Dispose();
    }

    private Activity SpanIn(Activity parent)
    {
        lock (this.spans)
        {
            return this.spans.Single(s => s.TraceId == parent.TraceId && s.Source.Name == ApplicationTelemetry.Name);
        }
    }
}
