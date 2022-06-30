namespace CleanWebApi.Presentation.Endpoints.Version;

using Errors;
using Common;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Entity = Application.Versions.Entities;
using Queries = Application.Versions.Queries;

[SwaggerTag("Lookup the application version details", "https://github.com/stphnwlsh/cleanwebapi")]
public class VersionController : ApiControllerBase
{
    private readonly ILogger<VersionController> logger;

    public VersionController(ILogger<VersionController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Lookup the application version details
    /// </summary>
    /// <returns>
    /// The current application version details
    /// </returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /Version
    ///
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(Entity.Version), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Entity.Version>> Get()
    {
        return await this.Mediator.Send(new Queries.GetVersion.GetVersionQuery());
    }
}
