namespace CleanWebApi.Application.Tests.Unit.Common.Behaviours;

using Application.Authors.Entities;
using Application.Authors.Queries.GetAuthorById;
using Application.Common.Behaviours;
using Application.Common.Enums;
using Application.Common.Exceptions;
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

public class LoggingBehaviourTests
{
    private readonly FakeLogger<LoggingBehaviour<GetAuthorByIdQuery, Author>> logger = new();
    private readonly FakeTimeProvider timeProvider = new();
    private readonly LoggingBehaviour<GetAuthorByIdQuery, Author> behaviour;
    private readonly GetAuthorByIdQuery query = new() { Id = Guid.NewGuid() };

    public LoggingBehaviourTests()
    {
        this.behaviour = new LoggingBehaviour<GetAuthorByIdQuery, Author>(this.logger, this.timeProvider);
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

        // Act
        var exception = await Should.ThrowAsync<ValidationException>(async () => await this.behaviour.Handle(this.query, next, CancellationToken.None));

        // Assert
        exception.Errors.ShouldNotBeEmpty();

        var record = this.logger.Collector.LatestRecord;

        this.logger.Collector.Count.ShouldBe(1);
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldBe("GetAuthorByIdQuery failed validation in 0.0 ms for Id, Name");
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

        // Act
        _ = await Should.ThrowAsync<NotFoundException>(async () => await this.behaviour.Handle(this.query, next, CancellationToken.None));

        // Assert
        var record = this.logger.Collector.LatestRecord;

        this.logger.Collector.Count.ShouldBe(1);
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldBe("GetAuthorByIdQuery found nothing in 3.0 ms: The Author with the supplied id was not found.");
    }

    [Fact]
    public async Task Handle_ShouldNotLog_WhenExceptionIsUnexpected()
    {
        // Arrange
        var next = Substitute.For<MessageHandlerDelegate<GetAuthorByIdQuery, Author>>();

        _ = next(default!, default).ThrowsAsyncForAnyArgs(new InvalidOperationException("Broken"));

        // Act
        _ = await Should.ThrowAsync<InvalidOperationException>(async () => await this.behaviour.Handle(this.query, next, CancellationToken.None));

        // Assert
        this.logger.Collector.Count.ShouldBe(0);
    }
}
