namespace CleanWebApi.Presentation.Tests.Integration.Controllers;

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Application.Common.Paging;
using Application.Movies.Entities;
using Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

public class MovieEndpointTests
{
    private static readonly WebApiApplication Application = new();

    [Fact]
    public async Task GetMovies_ShouldReturn_Ok()
    {
        // Arrange
        using var client = Application.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/movies");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<List<Movie>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();

        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(50);

        foreach (var movie in result)
        {
            _ = movie.ShouldNotBeNull();
            _ = movie.Id.ShouldBeOfType<Guid>();
            _ = movie.Title.ShouldBeOfType<string>();
            movie.Title.ShouldNotBeNullOrWhiteSpace();

            foreach (var review in movie.Reviews)
            {
                _ = review.Stars.ShouldBeOfType<int>();
                review.Stars.ShouldBeInRange(1, 5);

                _ = review.ReviewAuthor.ShouldNotBeNull();
                _ = review.ReviewAuthor.Id.ShouldBeOfType<Guid>();
                _ = review.ReviewAuthor.FirstName.ShouldBeOfType<string>();
                review.ReviewAuthor.FirstName.ShouldNotBeNullOrWhiteSpace();
                _ = review.ReviewAuthor.LastName.ShouldBeOfType<string>();
                review.ReviewAuthor.LastName.ShouldNotBeNullOrWhiteSpace();
            }
        }
    }

    [Fact]
    public async Task GetMovieById_ShouldReturn_Ok()
    {
        // Arrange
        using var client = Application.CreateClient();
        using var authorResponse = await client.GetAsync("/api/movies");
        var authorResult = (await authorResponse.Content.ReadAsStringAsync()).Deserialize<List<Movie>>().First(m => m.Reviews.Count > 0);

        // Act
        using var response = await client.GetAsync($"/api/movies/{authorResult.Id}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<Movie>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();

        result.Id.ShouldBe(authorResult.Id);
        _ = result.Title.ShouldBeOfType<string>();
        result.Title.ShouldNotBeNullOrWhiteSpace();
        result.Reviews.ShouldNotBeEmpty();

        foreach (var review in result.Reviews)
        {
            _ = review.Stars.ShouldBeOfType<int>();
            review.Stars.ShouldBeInRange(1, 5);

            _ = review.ReviewAuthor.ShouldNotBeNull();
            _ = review.ReviewAuthor.Id.ShouldBeOfType<Guid>();
            _ = review.ReviewAuthor.FirstName.ShouldBeOfType<string>();
            review.ReviewAuthor.FirstName.ShouldNotBeNullOrWhiteSpace();
            _ = review.ReviewAuthor.LastName.ShouldBeOfType<string>();
            review.ReviewAuthor.LastName.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetMovieById_ShouldReturn_ValidationProblem()
    {
        // Arrange
        using var client = Application.CreateClient();

        // Act
        using var response = await client.GetAsync($"/api/movies/{Guid.Empty}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<ValidationProblemDetails>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        _ = result.ShouldNotBeNull();

        result.Status.ShouldBe(StatusCodes.Status400BadRequest);
        result.Errors.ShouldContainKey("Id");
        result.Errors["Id"].ShouldBe(["A Movie Id was not supplied."]);
    }

    [Fact]
    public async Task GetMovieById_ShouldReturn_NotFound()
    {
        // Arrange
        using var client = Application.CreateClient();

        // Act
        using var response = await client.GetAsync($"/api/movies/{Guid.NewGuid()}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<ProblemDetails>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        _ = result.ShouldNotBeNull();

        result.Status.ShouldBe(StatusCodes.Status404NotFound);
        result.Detail.ShouldBe("The Movie with the supplied id was not found.");
    }

    [Fact]
    public async Task SearchMovies_ShouldReturn_FirstPage()
    {
        // Arrange
        using var client = Application.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/movies/search");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<Page<Movie>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();

        result.Items.Count.ShouldBe(Paging.DefaultLimit);
        result.TotalCount.ShouldBe(50);
        result.NextCursor.ShouldNotBeNullOrWhiteSpace();
        result.Items.Select(m => m.Title).ShouldBe(result.Items.Select(m => m.Title).Order());
    }

    [Fact]
    public async Task SearchMovies_ShouldReturn_NextPage()
    {
        // Arrange
        using var client = Application.CreateClient();
        using var firstResponse = await client.GetAsync("/api/movies/search?limit=20");
        var first = (await firstResponse.Content.ReadAsStringAsync()).Deserialize<Page<Movie>>();

        // Act
        using var response = await client.GetAsync($"/api/movies/search?limit=40&cursor={Uri.EscapeDataString(first.NextCursor)}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<Page<Movie>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();

        result.Items.Count.ShouldBe(30);
        result.TotalCount.ShouldBe(50);
        result.NextCursor.ShouldBeNull();
        result.Items.Select(m => m.Id).Intersect(first.Items.Select(m => m.Id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchMovies_ShouldReturn_Matches()
    {
        // Arrange
        using var client = Application.CreateClient();
        using var moviesResponse = await client.GetAsync("/api/movies");
        var title = (await moviesResponse.Content.ReadAsStringAsync())
            .Deserialize<List<Movie>>()
            .First().Title;

        // Act
        using var response = await client.GetAsync($"/api/movies/search?query={Uri.EscapeDataString(title.ToUpperInvariant())}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<Page<Movie>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();

        result.Items.ShouldNotBeEmpty();
        result.Items.ShouldAllBe(m => m.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("limit=0", "Limit", "A limit must be between 1 and 50.")]
    [InlineData("limit=51", "Limit", "A limit must be between 1 and 50.")]
    [InlineData("cursor=fake", "Cursor", "The cursor is not valid. Use the nextCursor from a previous page, or leave it empty for the first page.")]
    public async Task SearchMovies_ShouldReturn_ValidationProblem(string queryString, string key, string message)
    {
        // Arrange
        using var client = Application.CreateClient();

        // Act
        using var response = await client.GetAsync($"/api/movies/search?{queryString}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<ValidationProblemDetails>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        _ = result.ShouldNotBeNull();

        result.Status.ShouldBe(StatusCodes.Status400BadRequest);
        result.Errors.ShouldContainKey(key);
        result.Errors[key].ShouldBe([message]);
    }
}
