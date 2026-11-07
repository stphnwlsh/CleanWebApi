---
name: expose-a-use-case
description: How to put an Application use case on a Web API controller action - controllers on ApiControllerBase, the injected ISender, ActionResult return types, ProducesResponseType metadata, XML docs for Scalar and integration tests. Use when adding or changing an action in this Web API.
metadata:
  scope: presentation
---

# Expose a use case (Web API)

## The rule

Controllers live in `src/Presentation/Controllers/<Feature>/<Feature>Controller.cs`, one per resource, and inherit `ApiControllerBase`. The base class carries `[ApiController]`, `[Consumes("application/json")]` and, in a solution with authentication, `[Authorize]`, and takes the `ISender` through its constructor. Each action builds the Application request, sends it with `this.Sender` and returns an `ActionResult`. The action's XML comments become the operation's documentation in Scalar. Actions contain no logic: validation, not-found and everything else happen in Application, and `ApplicationExceptionHandler` turns exceptions into problem details.

## Steps

1. Add the use case first (`add-a-use-case`).
2. A new controller takes the sender in its primary constructor and passes it on:

   ```csharp
   [Route("api/reviews")]
   public class ReviewsController(ISender sender) : ApiControllerBase(sender)
   ```

3. Give each action its verb and route template, `[EndpointName(nameof(...))]`, and one `[ProducesResponseType]` per status with its content type: `application/json` for success bodies, `application/problem+json` for errors, none for `204`.
4. Parameters: route values by name (`Guid id`), `[FromQuery]` on query values, the body as a request class, then `CancellationToken cancellationToken`. Optional query parameters with defaults go last.
5. Return `ActionResult<T>` for actions with a body and `IActionResult` for `204`. Use `this.Ok(...)`, `this.CreatedAtAction(nameof(this.GetReviewById), new { id = response.Id }, response)` for creates, and `this.NoContent()` for updates and deletes.
6. Bodies use a request class in `Controllers/<Feature>/Requests/` with XML docs and `<example>` values. Map it to the command in the action.
7. Document the action: `<summary>` (short, title case, shown as the operation name), `<remarks>` with a sentence or two and a sample request, `<param>` for each input except the token, and a `<response code="...">` for each status.
8. Test it with real requests in `tests/Presentation.Tests.Integration/Controllers/<Feature>EndpointTests.cs` (`presentation-integration-tests`). Cover every documented status code (`test-design`).

## Example

```csharp
// src/Presentation/Controllers/Reviews/ReviewsController.cs
[HttpPost]
[EndpointName(nameof(CreateReview))]
[ProducesResponseType(typeof(Entities.Review), StatusCodes.Status201Created, "application/json")]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public async Task<ActionResult<Entities.Review>> CreateReview(CreateReviewRequest request, CancellationToken cancellationToken)
{
    var command = new Commands.CreateReview.CreateReviewCommand
    {
        AuthorId = request.AuthorId,
        MovieId = request.MovieId,
        Stars = request.Stars
    };

    var response = await this.Sender.Send(command, cancellationToken);

    return this.CreatedAtAction(nameof(this.GetReviewById), new { id = response.Id }, response);
}
```

## Don't

- Resolve services from `HttpContext.RequestServices`. Take them through the constructor.
- Catch exceptions, check `null` or read `ModelState` in an action. `[ApiController]` and the exception handler do that.
- Put `[Produces]` on the base class. It adds an empty JSON body to `204` responses; set the content type on each `[ProducesResponseType]` instead.
- Return an entity without `this.Ok(...)`. Keep every action's result explicit.
- Put `[Authorize]` on single actions. With authentication the base class protects everything and `[AllowAnonymous]` is the exception; without it there's nothing to protect (`optional-token-auth`).

## Check

``` bash
dotnet test tests/Presentation.Tests.Integration
dotnet run --project ./src/Presentation   # then read the action at http://localhost:7340/scalar
```
