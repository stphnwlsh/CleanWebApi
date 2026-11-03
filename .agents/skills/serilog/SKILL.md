---
name: serilog
description: How logging works with Serilog - registration with AddSerilog, configuration from appsettings, sinks per environment (console locally, Seq in Docker), the enrichers and properties on every event, request logging, levels, and message templates. Use when adding a log line, changing log configuration or levels, adding a property, or looking for where logs go.
metadata:
  scope: shared
---

# Serilog

## The rule

Serilog is the logger. Application and Infrastructure code log through `Microsoft.Extensions.Logging.ILogger<T>`, and Serilog does the writing. The presentation layer's startup registers it with `builder.Services.AddSerilog(...)`, which reads the `Serilog` section of the appsettings files and also sets the static `Log.Logger` that `Program.cs` uses for the start and fatal messages.

| Environment | Sink | Where to look |
| --- | --- | --- |
| `local` | Console, with the output template in `appsettings.local.json` | The terminal |
| `docker` | Seq at `http://seq:5341` | <http://localhost:5341> |

Every event carries these properties:

| Property | Comes from |
| --- | --- |
| `ApplicationName` | `Serilog:Properties` in the environment's appsettings |
| `EnvironmentName`, `MachineName` | `Serilog.Enrichers.Environment`, listed under `Serilog:Enrich` |
| `AssemblyVersion`, `AssemblyInformationalVersion` | Added in code at startup |
| Anything pushed with `LogContext` | `FromLogContext` |

Levels are set in appsettings, never in code: `Debug` by default, `Information` for `Microsoft`, `Warning` for `Microsoft.AspNetCore`, `Error` for `System`. The built-in `Logging` section is set to `None` so nothing is written twice.

Web templates log one line per request with `UseSerilogRequestLogging()`, under `Serilog.AspNetCore.RequestLoggingMiddleware`.

The Lambda template doesn't register a logger yet. CloudWatch Logs keeps what the Lambda runtime writes, including any unhandled exception.

## Steps

1. Take `ILogger<T>` through the constructor where you need it. Never reference Serilog types outside the presentation layer.
2. Write message templates with named holes: `logger.LogInformation("Created review {ReviewId} for movie {MovieId}", review.Id, movieId);`
3. Pick the level for the reader at 3am: `Information` for something that happened, `Warning` for something odd that was handled, `Error` for a failed request, `Debug` for detail you'd only want while developing.
4. Name new properties in PascalCase with no spaces, so they can be used in templates and queried in Seq without quoting.
5. Add a sink or change a level in the appsettings files. A new sink package goes in `Directory.Packages.props` (`packages-and-dependencies`).
6. In Seq, filter with property expressions: `ApplicationName = 'CleanWebApi-Docker' and @Level = 'Error'`.

## Example

```csharp
// src/Presentation/Extensions, where startup registers services
_ = builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    var assembly = Assembly.GetEntryAssembly();

    _ = loggerConfiguration.ReadFrom.Configuration(services.GetRequiredService<IConfiguration>())
        .Enrich.WithProperty(
            "AssemblyVersion",
            assembly?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);
});
```

```json
// src/Presentation/appsettings.docker.json
"Serilog": {
  "WriteTo": [ { "Name": "Seq", "Args": { "serverUrl": "http://seq:5341" } } ],
  "Properties": { "ApplicationName": "CleanWebApi-Docker" }
}
```

## Don't

- Use string interpolation in a log message (`$"Created {id}"`). It loses the property and allocates on every call.
- Log tokens, connection strings, passwords or personal data.
- Use `Console.WriteLine`.
- Reference Serilog from Application or Infrastructure.
- Register Serilog with `builder.Host.UseSerilog`. `AddSerilog` is the current registration.
- Add a second request logger, such as `UseHttpLogging`. Requests are logged once, by Serilog.

## Check

``` bash
dotnet run --project ./src/Presentation   # watch the console, then make a request
docker compose up -d --build              # then open http://localhost:5341
```
