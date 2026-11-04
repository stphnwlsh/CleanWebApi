---
name: telemetry
description: How OpenTelemetry traces, metrics and logs work - the standard OTEL_* settings that switch exporting on, the TelemetryBehaviour span and metrics for every use case, the database command spans, baggage, span events, what each presentation layer instruments, the Aspire dashboard in Docker Compose, and the signals we're waiting on. Use when adding a span, metric or tag, changing what's exported or where, reading telemetry locally, or adding a dependency that should be traced.
metadata:
  scope: shared
---

# Telemetry

## The rule

Telemetry is OpenTelemetry, and it leaves the app over OTLP and nothing else. No vendor SDK, no Aspire package, nothing that ties the code to one backend. Traces and metrics are always collected; they're exported, with the logs, only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Every standard `OTEL_*` setting works as the OpenTelemetry SDK documents it.

| Signal | What we send | Where it comes from |
| --- | --- | --- |
| Traces | A span for each incoming request, each use case and each SQL command, plus outgoing HTTP calls | Presentation instrumentation, `TelemetryBehaviour`, `DatabaseCommandInterceptor` |
| Metrics | Request counts and durations per use case, EF Core's own meter, ASP.NET Core, HttpClient and runtime metrics, with exemplars linking a measurement to a trace | `ApplicationTelemetry`, `Microsoft.EntityFrameworkCore`, Presentation instrumentation |
| Logs | Every Serilog event, carrying the trace and span IDs of whatever was running when it was written | Serilog's OpenTelemetry sink (`serilog`) |
| Baggage | Propagated with the trace context, and copied onto each use case span as tags | W3C `baggage` header, `TelemetryBehaviour` |
| Events | Span events for validation failures and missing entities, and `exception` events for errors | `TelemetryBehaviour`, `DatabaseCommandInterceptor` |

Each layer publishes under its own name, and only the presentation layer's startup knows about the OpenTelemetry SDK:

| Layer | Publishes | How |
| --- | --- | --- |
| Application | `ApplicationTelemetry.Name` activity source and meter: a span per request, `application.requests` and `application.request.duration` | `System.Diagnostics` only, so Application stays free of telemetry packages |
| Infrastructure | `InfrastructureTelemetry.Name` activity source: a client span per database command, with `db.system.name`, `db.namespace`, `db.operation.name` and the parameterised `db.query.text` | `DatabaseCommandInterceptor`, an EF Core `DbCommandInterceptor` |
| Infrastructure | `Microsoft.EntityFrameworkCore` meter: queries, saves, active contexts, compiled query cache | Built into EF Core; startup subscribes to `InfrastructureTelemetry.EntityFrameworkCoreMeterName` |
| Presentation | `service.name`, `service.version` and `deployment.environment.name` on everything, plus its own instrumentation | `TelemetrySettings` and the startup's Telemetry region |

What each presentation layer adds:

| Template | Instrumentation |
| --- | --- |
| Minimal API, Web API | ASP.NET Core and HttpClient traces and metrics, runtime metrics, the authentication and authorization meters. Health endpoints aren't traced |
| GraphQL | The same, plus Hot Chocolate's resolver and operation spans from `HotChocolate.Diagnostics` |
| MCP | The same, plus the MCP SDK's own `Experimental.ModelContextProtocol` spans and `mcp.*.operation.duration` metrics |
| Lambda | A span per invocation from `OpenTelemetry.Instrumentation.AWSLambda`, flushed before the function returns |

The app sends to an OpenTelemetry Collector, never straight to a backend, because that's how it should run in production: the collector owns batching, retries, sampling, scrubbing and the choice of backend, so none of that lives in the app. `otel-collector.yaml` holds its configuration: an OTLP receiver on 4317 (gRPC) and 4318 (HTTP), the `memory_limiter` and `batch` processors, and one exporter per backend.

Locally, Docker Compose runs the collector (`otel/opentelemetry-collector`) and the Aspire dashboard (`mcr.microsoft.com/dotnet/aspire-dashboard`), and the collector forwards everything to the dashboard. The dashboard is only a container that receives OTLP and shows logs, traces and metrics at <http://localhost:18888>; it keeps everything in memory, so it's empty after a restart. In production, run a collector beside the app or as a gateway and swap the dashboard exporter for your backend's.

## Steps

1. A new use case needs nothing: `TelemetryBehaviour` gives it a span and metrics. Its span is named after the request class.
2. To trace work inside Application or Infrastructure, start a child span from the layer's own `ActivitySource` with `using var activity = ApplicationTelemetry.ActivitySource.StartActivity("Name");`. Set tags only when `activity is { IsAllDataRequested: true }`, and use the [semantic convention](https://opentelemetry.io/docs/specs/semconv/) names where one exists.
3. To add a metric, add an instrument to `ApplicationTelemetry`, created from the `IMeterFactory` meter, with a unit and a description. Keep tag values to a small fixed set; never tag a metric with an id.
4. A new dependency in Infrastructure (a queue, an HTTP API) gets its spans from its own instrumentation package if a stable one exists. Subscribe to its source or meter name in the presentation layer's startup, and expose the name as a constant in `InfrastructureTelemetry`.
5. Use stable packages only. When the only instrumentation is a prerelease package, write the small piece we need, as `DatabaseCommandInterceptor` does, and add it to the watch list below.
6. Test spans with an `ActivityListener` that keeps only the test's own trace, and metrics with `MetricCollector<T>` from `Microsoft.Extensions.Diagnostics.Testing` (`unit-tests`). The integration tests send a `traceparent` header and check the request, use case and database spans join up.
7. To send telemetry somewhere new, add an exporter to `otel-collector.yaml` and list it in the pipelines. The app's settings don't change.
8. To see it locally, run `docker compose up -d --build` and open <http://localhost:18888>. For a `dotnet run`, start the collector with `docker compose up -d otel-collector` and set `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317`.

## Example

```csharp
// src/Application/Common/Behaviours/TelemetryBehaviour.cs
using var activity = ApplicationTelemetry.ActivitySource.StartActivity(RequestName);

if (activity is { IsAllDataRequested: true })
{
    _ = activity.SetTag(ApplicationTelemetry.TagNames.RequestName, RequestName);

    foreach (var (key, value) in activity.Baggage)
    {
        _ = activity.SetTag(key, value);
    }
}
```

```yaml
# docker-compose.yml
environment:
  OTEL_EXPORTER_OTLP_ENDPOINT: http://otel-collector:4317
  OTEL_EXPORTER_OTLP_PROTOCOL: grpc
```

```yaml
# otel-collector.yaml
exporters:
  otlp_grpc/aspire:
    endpoint: aspire-dashboard:18889
    tls:
      insecure: true
```

## Watch list

These signals and packages aren't stable for .NET yet. Check them when dependencies are updated, and adopt each one once it has a stable release:

- `OpenTelemetry.Instrumentation.EntityFrameworkCore`: replaces `DatabaseCommandInterceptor`.
- Profiles: there's no stable .NET SDK for the profiling signal.
- The OpenTelemetry Events API: until then, events are span events and `[LoggerMessage]` logs with event IDs.

## Don't

- Reference an OpenTelemetry, Aspire or vendor package from Application or Infrastructure. They publish with `System.Diagnostics`, and Presentation subscribes.
- Export anywhere but OTLP, or hard-code an endpoint. Settings come from the `OTEL_*` variables.
- Point the app straight at a backend, the dashboard included. It sends to a collector, and the collector decides where things go.
- Add a prerelease package to get a signal.
- Put ids, tokens or personal data in span names, tags or metric tags. `db.query.text` is safe only because EF Core sends parameters, not values.
- Mark a span as an error for a validation failure or a missing entity. The caller got an answer; record a span event instead.
- Trace the health endpoints. Probes would bury everything else.
- Start a span with `new Activity(...)`. Use the layer's `ActivitySource`, so nothing is recorded when nobody is listening.

## Check

``` bash
dotnet test --filter "FullyQualifiedName~Telemetry"   # behaviour, interceptor and end-to-end trace tests
docker compose up -d --build                         # then open http://localhost:18888 and make a request
```
