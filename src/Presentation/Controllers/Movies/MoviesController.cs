namespace CleanWebApi.Presentation.Controllers.Movies;

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
    [EndpointName(nameof(GetMovieById))]
    [ProducesResponseType(typeof(Entities.Movie), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Entities.Movie>> GetMovieById(Guid id)
    {
        return this.Ok(await this.Mediator.Send(new Queries.GetMovieById.GetMovieByIdQuery { Id = id }));
    }
}
