namespace CleanWebApi.Presentation.Controllers.Movies;

using Application.Common.Paging;
using Common;
using Microsoft.AspNetCore.Mvc;
using Entities = Application.Movies.Entities;
using Queries = Application.Movies.Queries;

[Route("api/movies")]
public class MoviesController : ApiControllerBase
{
    /// <summary>
    /// List Movies
    /// </summary>
    /// <remarks>
    /// Returns one page of movies, ordered by title, along with the reviews written about them. Add a search
    /// term to return only the movies whose title contains it, ignoring case. To fetch the next page, add the
    /// limit to the offset.
    ///
    /// Sample request:
    ///
    ///     GET /api/movies?query=the&amp;offset=0&amp;limit=10
    /// </remarks>
    /// <param name="query">The words to look for in the title, ignoring case. Leave it out to list every movie.</param>
    /// <param name="offset">The number of movies to skip, 0 for the first page.</param>
    /// <param name="limit">The largest number of movies to return, from 1 to 50.</param>
    /// <response code="200">One page of Movies.</response>
    /// <response code="400">The search term, offset or limit is not valid.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet]
    [EndpointName(nameof(GetMovies))]
    [ProducesResponseType(typeof(Page<Entities.Movie>), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Page<Entities.Movie>>> GetMovies(
        [FromQuery] string? query,
        CancellationToken cancellationToken,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = Paging.DefaultLimit)
    {
        return this.Ok(await this.Sender.Send(new Queries.GetMovies.GetMoviesQuery
        {
            Query = query,
            Offset = offset,
            Limit = limit
        }, cancellationToken));
    }

    /// <summary>
    /// Look up a Movie by its Id
    /// </summary>
    /// <remarks>
    /// Returns a single movie along with the reviews written about it.
    ///
    /// Sample request:
    ///
    ///     GET /api/movies/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// </remarks>
    /// <param name="id">The unique identifier of the movie.</param>
    /// <response code="200">The Movie with the specified Id.</response>
    /// <response code="400">The Id is not valid.</response>
    /// <response code="404">No Movie exists with the specified Id.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet("{id:guid}")]
    [EndpointName(nameof(GetMovieById))]
    [ProducesResponseType(typeof(Entities.Movie), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Entities.Movie>> GetMovieById(Guid id, CancellationToken cancellationToken)
    {
        return this.Ok(await this.Sender.Send(new Queries.GetMovieById.GetMovieByIdQuery { Id = id }, cancellationToken));
    }
}
