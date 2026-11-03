namespace CleanWebApi.Presentation.Controllers.Common;

using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The base for every controller: JSON in, a token required unless an action allows anonymous access, and the
/// sender that passes each request to its Application handler.
/// </summary>
/// <param name="sender">Sends commands and queries to their Application handlers.</param>
[ApiController]
[Authorize]
[Consumes("application/json")]
public abstract class ApiControllerBase(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;
}
