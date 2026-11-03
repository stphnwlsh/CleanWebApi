---
name: json-serialization
description: How this Web API reads and writes JSON with System.Text.Json - the one set of options in UseApiDefaults, applied to both the controllers and the HTTP JSON options, and how tests use the same options. Use when adding a request or response type, changing how JSON looks, or when a value serialises differently than expected.
metadata:
  scope: presentation
---

# JSON serialization (Web API)

## The rule

Every request and response goes through System.Text.Json with one set of options, defined once in `UseApiDefaults()` (`src/Presentation/Serialization/JsonSerializerOptionsExtensions.cs`):

| Setting | Effect |
| --- | --- |
| `PropertyNamingPolicy = CamelCase` | `averageStars`, not `AverageStars` |
| `PropertyNameCaseInsensitive = true` | Request bodies are read whatever the case of their property names |
| `DefaultIgnoreCondition = WhenWritingNull` | Null properties are left out of responses |
| `NumberHandling = Strict` | Numbers must be JSON numbers. `"5"` for an `int` is refused with a `400`, and OpenAPI shows `integer`, not `integer \| string` |
| `JsonStringEnumConverter(CamelCase)` | Enums are written and read as camelCase strings |

ASP.NET Core has two sets of JSON options, and startup applies `UseApiDefaults()` to both in `ProgramExtensions.ConfigureBuilder`:

| Options | Used by |
| --- | --- |
| MVC `JsonOptions` (`AddControllers().AddJsonOptions`) | Controller request bodies and responses, and the problem details `[ApiController]` writes |
| HTTP `JsonOptions` (`Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>`) | Problem details written outside a controller, such as a `404` for an unknown route |

Controllers serialise with reflection, so there's no source-generated context to keep up to date. The integration tests build their options with the same `UseApiDefaults()`, so they read responses exactly as a client would.

Responses are trees: lists return summaries, and an entity holds only the flat records it needs. There's no cycle handling, so a cycle fails loudly instead of being quietly cut.

## Steps

1. Change how JSON looks only in `UseApiDefaults()`. Both option sets pick it up.
2. Add a test in `tests/Presentation.Tests.Unit/Serialization/JsonSerializerOptionsExtensionsTests.cs` for any setting you change, and an integration test if a client would see the difference.
3. Never let a response type refer back to its parent. Return a flat record instead (`ReviewAuthor`, `ReviewedMovie`).

## Example

```csharp
// src/Presentation/Extensions/ProgramExtensions.cs
_ = builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.UseApiDefaults());

_ = builder.Services.Configure<JsonOptions>(options => options.SerializerOptions.UseApiDefaults());
```

```csharp
// tests/Presentation.Tests.Integration/Extensions/JsonSerializerExtensions.cs
private static readonly JsonSerializerOptions Options = new JsonSerializerOptions().UseApiDefaults();
```

## Don't

- Set serializer options anywhere but `UseApiDefaults()`, including in tests.
- Configure one set of JSON options and not the other.
- Add `ReferenceHandler.IgnoreCycles` or `Preserve` to hide a cycle. Change the response shape.
- Use Newtonsoft.Json or `AddNewtonsoftJson()`.
- Use `[JsonPropertyName]` to rename one property. The naming policy names them all.

## Check

``` bash
dotnet test tests/Presentation.Tests.Unit --filter "FullyQualifiedName~Serialization"
dotnet test tests/Presentation.Tests.Integration
```
