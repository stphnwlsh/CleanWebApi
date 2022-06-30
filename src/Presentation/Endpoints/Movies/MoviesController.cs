namespace CleanWebApi.Presentation.Endpoints.Movies;

using Errors;
using Common;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Entities = Application.Movies.Entities;
using Queries = Application.Movies.Queries;

[SwaggerTag("Lookup all Movies or find them by Id", "https://github.com/stphnwlsh/cleanwebapi")]
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
    /// <returns>
    /// A list of all the Movies
    /// </returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /Movies
    ///
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<Entities.Movie>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Entities.Movie>>> GetMovies()
    {
        return await this.Mediator.Send(new Queries.GetMovies.GetMoviesQuery());
    }

    /// <summary>
    /// Lookup a Movie by its Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>
    /// The Movie with the specified Id.
    /// </returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /Movies/00000000-0000-0000-0000-000000000000
    ///
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Entities.Movie), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Entities.Movie>> GetMoviesById(Guid id)
    {
        return this.Ok(await this.Mediator.Send(new Queries.GetMovieById.GetMovieByIdQuery { Id = id }));
    }
}
