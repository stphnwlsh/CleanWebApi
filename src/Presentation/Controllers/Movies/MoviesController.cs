namespace CleanWebApi.Presentation.Controllers.Movies;

using Application.Common.Paging;
using Common;
using Microsoft.AspNetCore.Mvc;
using Entities = Application.Movies.Entities;
using Queries = Application.Movies.Queries;

public class MoviesController : ApiControllerBase
{
    private readonly ILogger<MoviesController> logger;

    public MoviesController(ILogger<MoviesController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Lookup all Movies
    /// </summary>
    /// <remarks>
    /// Returns every movie along with the reviews written about it.
    ///
    /// Sample request:
    ///
    ///     GET /api/Movies
    /// </remarks>
    /// <response code="200">A list of all the Movies.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet]
    [EndpointName(nameof(GetMovies))]
    [ProducesResponseType(typeof(List<Entities.Movie>), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<IList<Entities.Movie>>> GetMovies()
    {
        return await this.Mediator.Send(new Queries.GetMovies.GetMoviesQuery());
    }

    /// <summary>
    /// Search Movies by title
    /// </summary>
    /// <remarks>
    /// Returns one page of movies whose title contains the search term, ignoring case, ordered by title.
    /// Leave the search term out to page through every movie. When more results remain, the response
    /// holds a nextCursor. Send it back as the cursor to fetch the next page.
    ///
    /// Sample request:
    ///
    ///     GET /api/Movies/search?query=the&amp;limit=10
    /// </remarks>
    /// <param name="query">The words to look for in the title, ignoring case. Leave it out to list every movie.</param>
    /// <param name="limit">The largest number of movies to return, from 1 to 50.</param>
    /// <param name="cursor">The nextCursor from a previous page. Leave it out for the first page.</param>
    /// <response code="200">One page of matching Movies.</response>
    /// <response code="400">The search term, limit or cursor is not valid.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet("search")]
    [EndpointName(nameof(SearchMovies))]
    [ProducesResponseType(typeof(Page<Entities.Movie>), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Page<Entities.Movie>>> SearchMovies(
        [FromQuery] string query,
        [FromQuery] int limit = Paging.DefaultLimit,
        [FromQuery] string cursor = null)
    {
        return this.Ok(await this.Mediator.Send(new Queries.SearchMovies.SearchMoviesQuery
        {
            Query = query,
            Limit = limit,
            Cursor = cursor
        }));
    }

    /// <summary>
    /// Lookup a Movie by its Id
    /// </summary>
    /// <remarks>
    /// Returns a single movie along with the reviews written about it.
    ///
    /// Sample request:
    ///
    ///     GET /api/Movies/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// </remarks>
    /// <param name="id">The unique identifier of the movie.</param>
    /// <response code="200">The Movie with the specified Id.</response>
    /// <response code="400">The Id is not valid.</response>
    /// <response code="404">No Movie exists with the specified Id.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet("{id:guid}")]
    [EndpointName("GetMovieById")]
    [ProducesResponseType(typeof(Entities.Movie), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Entities.Movie>> GetMoviesById(Guid id)
    {
        return this.Ok(await this.Mediator.Send(new Queries.GetMovieById.GetMovieByIdQuery { Id = id }));
    }
}
