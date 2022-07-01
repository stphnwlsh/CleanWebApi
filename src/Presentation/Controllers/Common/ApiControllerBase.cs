namespace CleanWebApi.Presentation.Controllers.Common;

using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private IMediator mediator = null!;

    protected IMediator Mediator => this.mediator ??= this.HttpContext.RequestServices.GetRequiredService<IMediator>();
}
