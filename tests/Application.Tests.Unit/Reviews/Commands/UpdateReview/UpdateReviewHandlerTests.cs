namespace CleanWebApi.Application.Tests.Unit.Reviews.Commands.UpdateReview;

using Application.Common.Exceptions;
using Application.Reviews;
using Application.Reviews.Commands.UpdateReview;
using Application.Reviews.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

public class UpdateReviewHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnUpdatedReview_ReviewExists()
    {
        // Arrange
        var command = new UpdateReviewCommand
        {
            Id = Guid.NewGuid(),
            Stars = 1
        };

        var review = new Review(command.Id, command.Stars);
        var reviewsRepository = Substitute.For<IReviewsRepository>();

        _ = reviewsRepository.UpdateReview(default, default, default, default).ReturnsForAnyArgs(review);

        var handler = new UpdateReviewHandler(reviewsRepository);
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        // Act
        var result = await handler.Handle(command, token);

        // Assert
        result.ShouldBe(review);

        _ = await reviewsRepository.Received(1).UpdateReview(command.Id, command.Stars, command.Text, token);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_ReviewDoesNotExist()
    {
        // Arrange
        var command = new UpdateReviewCommand
        {
            Id = Guid.NewGuid(),
            Stars = 5
        };

        var reviewsRepository = Substitute.For<IReviewsRepository>();

        _ = reviewsRepository.UpdateReview(default, default, default, default).ReturnsForAnyArgs(Task.FromResult<Review?>(null));

        var handler = new UpdateReviewHandler(reviewsRepository);
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(async () => await handler.Handle(command, token));

        // Assert
        exception.Message.ShouldBe("The Review with the supplied id was not found.");

        _ = await reviewsRepository.Received(1).UpdateReview(command.Id, command.Stars, command.Text, token);
    }
}
