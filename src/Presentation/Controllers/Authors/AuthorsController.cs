namespace CleanWebApi.Presentation.Controllers.Authors;

using Common;
using Errors;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Entities = Application.Authors.Entities;
using Queries = Application.Authors.Queries;

[SwaggerTag("Lookup all Movies or find them by Id", "https://github.com/stphnwlsh/cleanwebapi")]
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
    /// <returns>
    /// A list of all the Authors
    /// </returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /Authors
    ///
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<Entities.Author>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Entities.Author>>> GetAuthors()
    {
        return await this.Mediator.Send(new Queries.GetAuthors.GetAuthorsQuery());
    }

    /// <summary>
    /// Lookup an Author by their Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>
    /// The Author with the specified id.
    /// </returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /Authors/00000000-0000-0000-0000-000000000000
    ///
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Entities.Author), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Entities.Author>> GetAuthorsById(Guid id)
    {
        return this.Ok(await this.Mediator.Send(new Queries.GetAuthorById.GetAuthorByIdQuery { Id = id }));
    }
}
