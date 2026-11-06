---
name: openapi-and-scalar
description: How this Web API builds its OpenAPI document and serves the Scalar API reference - AddOpenApi, the document and operation transformers, the bearer scheme when authentication is on, the Scalar options, and the tests that read the generated document. Use when changing the API description, tags, security scheme, Scalar settings, or when the API reference looks wrong.
metadata:
  scope: presentation
---

# OpenAPI and Scalar (Web API)

## The rule

The OpenAPI document is generated at runtime by `Microsoft.AspNetCore.OpenApi` and served at `/openapi/v1.json`. Scalar (`Scalar.AspNetCore`) renders it at `/scalar`. Operation and schema text comes from XML comments on the actions and request classes (`xml-docs-and-openapi`), and each operation's tag comes from its controller name. Everything else is set by transformers in `src/Presentation/OpenApi/`.

| Transformer | Adds |
| --- | --- |
| `DocumentInfoTransformer` | Title (with the environment name), description, contact, licence, external docs and the tag descriptions |
| `BearerSecurityTransformer` | Only in a solution with authentication, and only when `Authentication:Authority` is set: the `Bearer` security scheme, a security requirement and a `401` response on every operation that needs a token |

Scalar is set up in `ProgramExtensions.ConfigureApplication` with the title, C# `HttpClient` as the default code sample, and its developer tools, agent, MCP and telemetry features turned off.

`tests/Presentation.Tests.Integration/OpenApi/OpenApiDocumentTests.cs` reads the generated document and fails if an operation has no summary, description, sample request or parameter descriptions, or a schema property has no description.

## Steps

1. A new controller needs a `CreateTag(...)` line in `DocumentInfoTransformer`, named like the controller without `Controller`, with a one-sentence description.
2. Something every operation needs goes in an operation transformer; something about the whole API goes in a document transformer. Register it in `AddOpenApi` in `ProgramExtensions.ConfigureBuilder`.
3. Transformers are classes implementing `IOpenApiDocumentTransformer` or `IOpenApiOperationTransformer`, `sealed`, with XML docs. Unit test them in `tests/Presentation.Tests.Unit/OpenApi/`.
4. When you add an action, update the operation count in `GetOpenApiDocument_ShouldDescribe_EveryOperation`.
5. Run the app and read the result at <http://localhost:8740/scalar>.

## Example

```csharp
// src/Presentation/Extensions/ProgramExtensions.cs
_ = builder.Services.AddOpenApi(options =>
{
    _ = options.AddDocumentTransformer<DocumentInfoTransformer>();

    // Only with --include-auth.
    if (authentication.Enabled)
    {
        _ = options.AddDocumentTransformer<BearerSecurityTransformer>();
        _ = options.AddOperationTransformer<BearerSecurityTransformer>();
    }
});
```

```csharp
// src/Presentation/Extensions/ProgramExtensions.cs
_ = app.MapOpenApi();
_ = app.MapScalarApiReference(options => options
    .WithTitle("CleanWebApi API")
    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
    .HideDeveloperTools()
    .DisableAgent()
    .DisableMcp()
    .DisableTelemetry());
```

## Don't

- Add Swashbuckle or NSwag. The built-in generator and Scalar replace them.
- Describe operations with `[EndpointSummary]` or `[EndpointDescription]`. The XML comments are the one source.
- Set `[Tags]` on an action. The controller name is the tag.
- Turn Scalar telemetry back on.
- Add the security scheme when authentication is off. The reference would ask for a token nobody needs.

## Check

``` bash
dotnet test --filter "FullyQualifiedName~OpenApi"
dotnet run --project ./src/Presentation   # then open http://localhost:8740/scalar
```
