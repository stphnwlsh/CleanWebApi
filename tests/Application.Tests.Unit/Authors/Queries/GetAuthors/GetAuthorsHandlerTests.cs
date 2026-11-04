namespace CleanWebApi.Application.Tests.Unit.Authors.Queries.GetAuthors;

using Application.Authors;
using Application.Authors.Entities;
using Application.Authors.Queries.GetAuthors;
using Application.Common.Paging;
using NSubstitute;
using Shouldly;
using Xunit;

public class GetAuthorsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturn_Page()
    {
        // Arrange
        var query = new GetAuthorsQuery { Query = "Lorem", Offset = 20, Limit = 5 };

        var repository = Substitute.For<IAuthorsRepository>();
        var handler = new GetAuthorsHandler(repository);
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        _ = repository.GetAuthors("Lorem", new PageRequest(20, 5), token).Returns(new Page<AuthorSummary>([new AuthorSummary(Guid.Empty, "First", "Last", 0, null)], 20, 5, 21));

        // Act
        var result = await handler.Handle(query, token);

        // Assert
        _ = await repository.Received(1).GetAuthors("Lorem", new PageRequest(20, 5), token);

        _ = result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items[0].FirstName.ShouldBe("First");
        result.Offset.ShouldBe(20);
        result.Limit.ShouldBe(5);
        result.TotalCount.ShouldBe(21);
    }

    [Fact]
    public async Task Handle_ShouldStartAt_FirstPage()
    {
        // Arrange
        var query = new GetAuthorsQuery();

        var repository = Substitute.For<IAuthorsRepository>();
        var handler = new GetAuthorsHandler(repository);
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        _ = repository.GetAuthors(null, new PageRequest(0, Paging.DefaultLimit), token).Returns(new Page<AuthorSummary>([new AuthorSummary(Guid.Empty, "First", "Last", 0, null)], 0, Paging.DefaultLimit, 1));

        // Act
        var result = await handler.Handle(query, token);

        // Assert
        _ = await repository.Received(1).GetAuthors(null, new PageRequest(0, Paging.DefaultLimit), token);

        result.Offset.ShouldBe(0);
        result.Limit.ShouldBe(Paging.DefaultLimit);
        result.TotalCount.ShouldBe(1);
    }
}
