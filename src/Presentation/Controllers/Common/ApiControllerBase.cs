namespace CleanWebApi.Presentation.Controllers.Common;

using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Consumes("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private IMediator mediator = null!;

    protected IMediator Mediator => this.mediator ??= this.HttpContext.RequestServices.GetRequiredService<IMediator>();
}
