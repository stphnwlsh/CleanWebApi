namespace CleanWebApi.Presentation.Controllers.Common;

using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Consumes("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? sender;

    protected ISender Sender => this.sender ??= this.HttpContext.RequestServices.GetRequiredService<ISender>();
}
