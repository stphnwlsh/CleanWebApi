# Running Clean Web API

There are two pre-configured environments, `local` and `docker`. Logs go to the console in both. Traces, metrics and logs also leave over OTLP whenever `OTEL_EXPORTER_OTLP_ENDPOINT` is set, which Docker Compose does for you.

## Database

The app stores its data in a SQLite file, `movie-reviews.db`, next to the app. In Docker the file is `/app/data/movie-reviews.db`, on the `movie-reviews-data` volume, so it survives a restart. On startup the app applies any pending Entity Framework Core migrations. When `Database:SeedSampleData` is `true`, it also seeds the same sample data into an empty database. It's on in `appsettings.local.json` and `appsettings.docker.json` and off everywhere else, so a deployed copy never starts with fake data. Delete the file, or run `docker compose down -v`, to start again. Change `ConnectionStrings:MovieReviews` to point somewhere else, or to `Data Source=MovieReviews;Mode=Memory;Cache=Shared` to keep everything in memory.

To add a migration after changing the data model:

``` bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Name> --project ./src/Infrastructure --output-dir Databases/MovieReviews/Migrations
```

## .NET CLI

`launchSettings.json` sets the ports, environment and logging, so one command is enough.

``` bash
dotnet run --project ./src/Presentation
```

## Docker Compose

`docker-compose.yml` starts the app, and the [Aspire dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/standalone), which shows its logs, traces and metrics. The app sends everything to an [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/), and the collector forwards it to the dashboard, the same shape as a production setup. The dashboard is only a container that receives OTLP; nothing in the solution depends on Aspire. The dashboard has no login and the collector accepts OTLP from anyone on ports 7343 and 7344. That's on purpose for a developer's machine, so don't run this compose file anywhere other people can reach.

``` bash
docker compose up -d --build
```

- App: <http://localhost:7340/scalar> (Scalar API reference)
- OpenAPI document: <http://localhost:7340/openapi/v1.json>
- Aspire dashboard: <http://localhost:7342>
- Health checks: <http://localhost:7340/health/live> (the process is up) and <http://localhost:7340/health/ready> (the database can be reached). Both answer without a token. The image's `HEALTHCHECK` uses the live one.

The container runs as the image's non-root `app` user.

To shut the containers down:

``` bash
docker compose down
```

## Ports

Every Lean Mean Clean template has its own block of ten ports on your machine, so this solution runs beside one made from another template without a clash. This one uses 7340 to 7349:

| Port | What listens |
| --- | --- |
| 7340 | The app over HTTP, from Docker Compose or `dotnet run` |
| 7341 | The app over HTTPS, `dotnet run` only |
| 7342 | The Aspire dashboard |
| 7343 | The OpenTelemetry Collector, OTLP over gRPC |
| 7344 | The OpenTelemetry Collector, OTLP over HTTP |

The app listens on 7340 everywhere: under `dotnet run`, inside its container and on your machine, so `docker run -p 7340:7340` works the same way. The dashboard and collector keep their usual ports inside their containers. To move the app, change `applicationUrl` in `launchSettings.json`, `ASPNETCORE_HTTP_PORTS`, `EXPOSE` and the `HEALTHCHECK` in the `Dockerfile`, and its mapping in `docker-compose.yml`. When you deploy the image, point the platform's target port at 7340. 7345 to 7349 are spare for anything you add.

## Telemetry

The app always collects [OpenTelemetry](https://opentelemetry.io) traces and metrics, and exports them, along with its logs, only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Every other standard `OTEL_*` variable works too, such as `OTEL_SERVICE_NAME`, `OTEL_EXPORTER_OTLP_PROTOCOL` and `OTEL_EXPORTER_OTLP_HEADERS`, so sending it to Grafana, Honeycomb, Seq or a collector is a settings change.

In production, send the app's telemetry to a collector rather than straight to a backend. Run one beside the app (a sidecar or agent) or as a shared gateway, and give it your backend's exporter. The collector then owns batching, retries, sampling and scrubbing, and changing backend never touches the app. `otel-collector.yaml` is a starting point: an OTLP receiver, the memory limiter and batch processors, and one exporter to swap.

To see a `dotnet run` in the dashboard, start the collector and the dashboard, and point the app at the collector:

``` bash
docker compose up -d otel-collector
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:7343 dotnet run --project ./src/Presentation
```

Every span, metric and log carries `service.version`, so you can always tell which build sent it.

## HTTPS

Outside the `local` and `Development` environments the app sends an HSTS header and redirects HTTP to HTTPS. The container only listens on HTTP, so in production put it behind a proxy or load balancer that ends TLS, and set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so the app knows the original request was HTTPS.

## Tests

``` bash
dotnet test
```

The tests never touch `movie-reviews.db`. Each test builds its own in-memory SQLite database from the migrations and the same fixed seed, so no test can change what another one sees and the results are the same every time.
