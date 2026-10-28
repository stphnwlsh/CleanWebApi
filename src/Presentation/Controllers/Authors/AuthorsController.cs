namespace CleanWebApi.Presentation.Controllers.Authors;

using Common;
using Microsoft.AspNetCore.Mvc;
using Entities = Application.Authors.Entities;
using Queries = Application.Authors.Queries;

public class AuthorsController : ApiControllerBase
{
    private readonly ILogger<AuthorsController> logger;

    public AuthorsController(ILogger<AuthorsController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Lookup all Authors
    /// </summary>
    /// <remarks>
    /// Returns every author along with the reviews they have written.
    ///
    /// Sample request:
    ///
    ///     GET /api/Authors
    /// </remarks>
    /// <response code="200">A list of all the Authors.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet]
    [EndpointName(nameof(GetAuthors))]
    [ProducesResponseType(typeof(List<Entities.Author>), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<IList<Entities.Author>>> GetAuthors()
    {
        return await this.Mediator.Send(new Queries.GetAuthors.GetAuthorsQuery());
    }

    /// <summary>
    /// Lookup an Author by their Id
    /// </summary>
    /// <remarks>
    /// Returns a single author along with the reviews they have written.
    ///
    /// Sample request:
    ///
    ///     GET /api/Authors/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// </remarks>
    /// <param name="id">The unique identifier of the author.</param>
    /// <response code="200">The Author with the specified Id.</response>
    /// <response code="400">The Id is not valid.</response>
    /// <response code="404">No Author exists with the specified Id.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet("{id:guid}")]
    [EndpointName("GetAuthorById")]
    [ProducesResponseType(typeof(Entities.Author), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Entities.Author>> GetAuthorsById(Guid id)
    {
        return this.Ok(await this.Mediator.Send(new Queries.GetAuthorById.GetAuthorByIdQuery { Id = id }));
    }
}
