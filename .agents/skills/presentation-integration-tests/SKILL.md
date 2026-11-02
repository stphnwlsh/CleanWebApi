---
name: presentation-integration-tests
description: How this Web API is tested end to end with WebApplicationFactory - the WebApiApplication host, a private database per test class, overriding settings and services, the JSON helper, asserting problem details, testing with authentication on and off, and where the presentation classes are tested directly. Use when writing or fixing a test in tests/Presentation.Tests.Integration.
metadata:
  scope: presentation
---

# Presentation integration tests (Web API)

## The rule

`tests/Presentation.Tests.Integration` makes real HTTP requests to the whole application in memory. `WebApiApplication` (a `WebApplicationFactory<Program>`) starts the app with the `local` environment, its own shared-cache in-memory SQLite database and the sample data on, so tests never touch a file or each other.

This template has no Presentation unit test project, so the same project also tests presentation classes directly: `ApplicationExceptionHandler`, `AuthenticationSettings`, the OpenAPI transformers and `UseApiDefaults()`.

Each test class that makes requests creates one `WebApiApplication` and disposes it. The constructor takes three optional arguments:

| Argument | Use it to |
| --- | --- |
| `environment` | Run as another environment, such as `docker` |
| `settings` | Override configuration in memory, such as `Authentication:Authority` |
| `configureServices` | Replace or post-configure services, such as the JWT signing key |

Responses are read with the `Deserialize<T>()` helper in `Extensions/JsonSerializerExtensions.cs`, which uses the API's own JSON options (`json-serialization`).

## Steps

1. Create `<Feature>EndpointTests.cs` in `Controllers/`, `IDisposable`, with `private readonly WebApiApplication application;` created in the constructor and disposed in `Dispose(bool)`.
2. In each test, `using var client = this.application.CreateClient();`, then send the request with `using var response = ...`.
3. Assert the status code first, then the body. Read success bodies as the Application entity, failures as `ProblemDetails` or `ValidationProblemDetails`, and check `Errors` or `Detail` exactly.
4. Find the rows you need through the API (list, then pick one with the property you need), not by hard-coded ids. Sample data ids are random.
5. Write one test per documented status code of the action (`test-design`), plus one for a body that can't be read, such as a number sent as a string.
6. Authentication: tests that need it pass `Authentication:Authority` and `Authentication:Audience` in `settings`, and swap the signing key in `configureServices`. See `Authentication/AuthenticationTests.cs`.

## Example

```csharp
// tests/Presentation.Tests.Integration/Controllers/ReviewEndpointTests.cs
[Fact]
public async Task CreateReview_ShouldReturn_ValidationProblem()
{
    // Arrange
    using var client = this.application.CreateClient();
    var json = (new { Stars = 0, AuthorId = Guid.Empty, MovieId = Guid.Empty }).Serialize();
    using var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    using var response = await client.PostAsync("/api/reviews", content);
    var result = (await response.Content.ReadAsStringAsync()).Deserialize<ValidationProblemDetails>();

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    _ = result.ShouldNotBeNull();
    result.Errors["Stars"].ShouldBe(["A star rating must be between 1 and 5."]);
}
```

```csharp
// tests/Presentation.Tests.Integration/Authentication/AuthenticationTests.cs
private readonly WebApiApplication application = new(
    settings: new Dictionary<string, string?>
    {
        ["Authentication:Authority"] = Authority,
        ["Authentication:Audience"] = Audience
    },
    configureServices: services => services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Configuration = new OpenIdConnectConfiguration { Issuer = Authority };
        options.TokenValidationParameters.IssuerSigningKey = SigningKey;
        options.TokenValidationParameters.ValidIssuer = Authority;
    }));
```

## Don't

- Share one application across test classes with `IClassFixture` or a `static` field. Each class gets its own database.
- Edit appsettings files for a test. Use `settings`.
- Build your own `JsonSerializerOptions` in a test.
- Assert only the status code when the action returns a body.
- Depend on the order rows come back in unless the action promises one.

## Check

``` bash
dotnet test tests/Presentation.Tests.Integration
```
