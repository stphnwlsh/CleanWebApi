namespace CleanWebApi.Application.Tests.Unit.Reviews.Queries.GetReviews;

using System.Threading;
using System.Threading.Tasks;
using Application.Reviews;
using CleanWebApi.Application.Reviews.Entities;
using CleanWebApi.Application.Reviews.Queries.GetReviews;
using NSubstitute;
using Shouldly;
using Xunit;

public class GetReviewsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPassThrough_Query()
    {
        // Arrange
        var query = new GetReviewsQuery();

        var context = Substitute.For<IReviewsRepository>();
        var handler = new GetReviewsHandler(context);
        var token = new CancellationTokenSource().Token;

        _ = context.GetReviews(token).Returns(new List<Review> {
            new Review{
                Id = Guid.Empty,
                Stars = 5
            }
        });

        // Act
        var result = await handler.Handle(query, token);

        // Assert
        _ = await context.Received(1).GetReviews(token);

        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(Guid.Empty);
        result[0].Stars.ShouldBe(5);
    }
}