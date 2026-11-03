namespace CleanWebApi.Application.Tests.Unit.Reviews.Commands.DeleteReview;

using Application.Common.Exceptions;
using Application.Reviews;
using Application.Reviews.Commands.DeleteReview;
using NSubstitute;
using Shouldly;
using Xunit;

public class DeleteReviewHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDeleteReview_ReviewExists()
    {
        // Arrange
        var command = new DeleteReviewCommand { Id = Guid.NewGuid() };
        var reviewsRepository = Substitute.For<IReviewsRepository>();

        _ = reviewsRepository.DeleteReview(default, default).ReturnsForAnyArgs(true);

        var handler = new DeleteReviewHandler(reviewsRepository);
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        // Act
        _ = await handler.Handle(command, token);

        // Assert
        _ = await reviewsRepository.Received(1).DeleteReview(command.Id, token);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_ReviewDoesNotExist()
    {
        // Arrange
        var command = new DeleteReviewCommand { Id = Guid.NewGuid() };
        var reviewsRepository = Substitute.For<IReviewsRepository>();

        _ = reviewsRepository.DeleteReview(default, default).ReturnsForAnyArgs(false);

        var handler = new DeleteReviewHandler(reviewsRepository);
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(async () => await handler.Handle(command, token));

        // Assert
        exception.Message.ShouldBe("The Review with the supplied id was not found.");

        _ = await reviewsRepository.Received(1).DeleteReview(command.Id, token);
    }
}
