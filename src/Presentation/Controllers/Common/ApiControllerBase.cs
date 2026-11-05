namespace CleanWebApi.Presentation.Controllers.Common;

using Mediator;
#if includeAuth
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The base for every controller: JSON in, and the sender that passes each request to its Application handler.
/// </summary>
/// <param name="sender">Sends commands and queries to their Application handlers.</param>
[ApiController]
#if includeAuth
// Every action needs a token unless it allows anonymous access.
[Authorize]
#endif
[Consumes("application/json")]
public abstract class ApiControllerBase(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;
}
