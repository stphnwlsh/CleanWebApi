namespace CleanWebApi.Presentation.Controllers.Authors;

using Application.Common.Paging;
using Common;
using Microsoft.AspNetCore.Mvc;
using Entities = Application.Authors.Entities;
using Queries = Application.Authors.Queries;

[Route("api/authors")]
public class AuthorsController : ApiControllerBase
{
    /// <summary>
    /// List Authors
    /// </summary>
    /// <remarks>
    /// Returns one page of authors, ordered by last name then first name, along with the reviews they have
    /// written. Add a search term to return only the authors whose first, last or full name contains it,
    /// ignoring case. To fetch the next page, add the limit to the offset.
    ///
    /// Sample request:
    ///
    ///     GET /api/authors?query=smith&amp;offset=0&amp;limit=10
    /// </remarks>
    /// <param name="query">The words to look for in the name, ignoring case. Leave it out to list every author.</param>
    /// <param name="offset">The number of authors to skip, 0 for the first page.</param>
    /// <param name="limit">The largest number of authors to return, from 1 to 50.</param>
    /// <response code="200">One page of Authors.</response>
    /// <response code="400">The search term, offset or limit is not valid.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet]
    [EndpointName(nameof(GetAuthors))]
    [ProducesResponseType(typeof(Page<Entities.Author>), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Page<Entities.Author>>> GetAuthors(
        [FromQuery] string? query,
        CancellationToken cancellationToken,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = Paging.DefaultLimit)
    {
        return this.Ok(await this.Sender.Send(new Queries.GetAuthors.GetAuthorsQuery
        {
            Query = query,
            Offset = offset,
            Limit = limit
        }, cancellationToken));
    }

    /// <summary>
    /// Look up an Author by their Id
    /// </summary>
    /// <remarks>
    /// Returns a single author along with the reviews they have written.
    ///
    /// Sample request:
    ///
    ///     GET /api/authors/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// </remarks>
    /// <param name="id">The unique identifier of the author.</param>
    /// <response code="200">The Author with the specified Id.</response>
    /// <response code="400">The Id is not valid.</response>
    /// <response code="404">No Author exists with the specified Id.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet("{id:guid}")]
    [EndpointName(nameof(GetAuthorById))]
    [ProducesResponseType(typeof(Entities.Author), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Entities.Author>> GetAuthorById(Guid id, CancellationToken cancellationToken)
    {
        return this.Ok(await this.Sender.Send(new Queries.GetAuthorById.GetAuthorByIdQuery { Id = id }, cancellationToken));
    }
}
