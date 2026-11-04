namespace CleanWebApi.Application.Tests.Unit.Movies.Queries.GetMovies;

using Application.Common.Paging;
using Application.Movies;
using Application.Movies.Entities;
using Application.Movies.Queries.GetMovies;
using NSubstitute;
using Shouldly;
using Xunit;

public class GetMoviesHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturn_Page()
    {
        // Arrange
        var query = new GetMoviesQuery { Query = "Lorem", Offset = 20, Limit = 5 };

        var repository = Substitute.For<IMoviesRepository>();
        var handler = new GetMoviesHandler(repository);
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        _ = repository.GetMovies("Lorem", new PageRequest(20, 5), token).Returns(new Page<MovieSummary>([new MovieSummary(Guid.Empty, "Title", 0, null)], 20, 5, 21));

        // Act
        var result = await handler.Handle(query, token);

        // Assert
        _ = await repository.Received(1).GetMovies("Lorem", new PageRequest(20, 5), token);

        _ = result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items[0].Title.ShouldBe("Title");
        result.Offset.ShouldBe(20);
        result.Limit.ShouldBe(5);
        result.TotalCount.ShouldBe(21);
    }

    [Fact]
    public async Task Handle_ShouldStartAt_FirstPage()
    {
        // Arrange
        var query = new GetMoviesQuery();

        var repository = Substitute.For<IMoviesRepository>();
        var handler = new GetMoviesHandler(repository);
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        _ = repository.GetMovies(null, new PageRequest(0, Paging.DefaultLimit), token).Returns(new Page<MovieSummary>([new MovieSummary(Guid.Empty, "Title", 0, null)], 0, Paging.DefaultLimit, 1));

        // Act
        var result = await handler.Handle(query, token);

        // Assert
        _ = await repository.Received(1).GetMovies(null, new PageRequest(0, Paging.DefaultLimit), token);

        result.Offset.ShouldBe(0);
        result.Limit.ShouldBe(Paging.DefaultLimit);
        result.TotalCount.ShouldBe(1);
    }
}
