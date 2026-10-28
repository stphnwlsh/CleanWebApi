namespace CleanWebApi.Presentation.Controllers.Common;

using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Consumes("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ISender Sender => this.HttpContext.RequestServices.GetRequiredService<ISender>();
}
