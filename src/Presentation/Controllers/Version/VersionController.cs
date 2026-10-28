namespace CleanWebApi.Presentation.Controllers.Version;

using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entity = Application.Versions.Entities;
using Queries = Application.Versions.Queries;

[AllowAnonymous]
[Route("api/version")]
public class VersionController : ApiControllerBase
{
    /// <summary>
    /// Look up the application version details
    /// </summary>
    /// <remarks>
    /// Returns the file and informational versions of the running application.
    ///
    /// Sample request:
    ///
    ///     GET /api/version
    /// </remarks>
    /// <response code="200">The current application version details.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet]
    [EndpointName("GetVersion")]
    [ProducesResponseType(typeof(Entity.Version), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Entity.Version>> Get(CancellationToken cancellationToken)
    {
        return await this.Sender.Send(new Queries.GetVersion.GetVersionQuery(), cancellationToken);
    }
}
