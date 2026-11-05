[![Clean Web API](https://raw.githubusercontent.com/LeanMeanCleanApps/brand/main/assets/banners/png/webapi.png)](https://leanmeancleanapps.github.io/webapi/)

[![Build Pipeline](https://img.shields.io/github/actions/workflow/status/LeanMeanCleanApps/CleanWebApi/build-pipeline.yml?branch=main&label=Build%20Pipeline&logo=github&style=for-the-badge)](https://github.com/LeanMeanCleanApps/CleanWebApi/actions/workflows/build-pipeline.yml)
[![NuGet](https://img.shields.io/nuget/v/CleanWebApi.Template?label=NuGet&logo=nuget&logoColor=white&style=for-the-badge)](https://www.nuget.org/packages/CleanWebApi.Template)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CleanWebApi.Template?label=Downloads&logo=nuget&logoColor=white&style=for-the-badge)](https://www.nuget.org/packages/CleanWebApi.Template)
[![License](https://img.shields.io/github/license/LeanMeanCleanApps/CleanWebApi?label=License&style=for-the-badge)](LICENSE.md)

A `dotnet new` template for an HTTP API using [ASP.NET Core controllers](https://learn.microsoft.com/aspnet/core/web-api/) in .NET 10, built with a streamlined Clean Architecture. All the layers you need, none of the ceremony you don't.

It's one of five [Lean Mean Clean Apps](https://leanmeancleanapps.github.io/) templates. They share the same Application and Infrastructure layers and differ only in how they face the world. Same genes, different haircuts.

The guides, the reference and the reasoning behind every opinion live at **[leanmeancleanapps.github.io/webapi](https://leanmeancleanapps.github.io/webapi/)**. This is the version for people who skim. We see you.

## Get started

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). [Docker](https://www.docker.com/products/docker-desktop) is optional, but it's the quickest way to see it running.

```bash
# Install the template
dotnet new install CleanWebApi.Template

# Create a solution
dotnet new cleanwebapi --name WorksOnMyMachine.Movies --authorName "Your Name"
cd WorksOnMyMachine.Movies
```

| Option | Default | What it does |
| --- | --- | --- |
| `--name`, `-n` | The folder's name | Names the solution and its root namespace |
| `--authorName`, `-an` | `Stephen Walsh` | Puts your name on the assembly instead of ours |
| `--include-auth` | Off | Adds JWT bearer token authentication. Leave it out and the API has no auth code at all |
| `--authority`, `-iss` | Empty | Your OAuth authorization server. Required with `--include-auth` |
| `--audience`, `-aud` | Empty | The audience tokens must be issued for, usually the API's own URL. Only used with `--include-auth` |
| `--no-restore` | Off | Skips the package restore that normally runs once the solution is created |

Rather click than type? Visual Studio and Rider show the same options in their new project dialogs, and `dotnet new cleanwebapi --help` lists them in the terminal.

That's three layers, four test projects, a Dockerfile and a CI pipeline. Go and tell your team you've been working on it all week.

## Run it

```bash
# Build the image and start it with sample data, plus a collector and the Aspire dashboard for its telemetry
docker compose up -d --build

# Ask it for a page of movies
curl "http://localhost:8080/api/movies?query=global&limit=2"
```

```json
{
  "items": [
    {
      "id": "30fca414-247a-779e-105a-d81aa2506f58",
      "title": "Assimilated global infrastructure",
      "reviewCount": 1,
      "averageStars": 1
    },
    {
      "id": "c27d6600-79a6-9346-2595-a52b7f07dc3e",
      "title": "Automated global alliance",
      "reviewCount": 3,
      "averageStars": 3.3
    }
  ],
  "offset": 0,
  "limit": 2,
  "totalCount": 2
}
```

Yes, the sample movies are named by a corporate buzzword generator, and the critics are harsh.

Every list endpoint returns one page at a time, with its `offset`, `limit` and a `totalCount`, so you always know how much more there is. Movies and authors come with their review count and average stars, worked out by the database rather than in a loop somewhere. Ask for one by id and its reviews come along for the ride.

Open [Scalar](https://github.com/scalar/scalar) at <http://localhost:8080/scalar> to browse and try every endpoint. Each one is documented with XML comments that the .NET 10 OpenAPI generator reads at build time, so the document at `/openapi/v1.json` can't drift from the code. Its logs, traces and metrics land in the [Aspire dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/standalone) at <http://localhost:18888>.

No Docker? `dotnet run --project src/Presentation` works too. Every other way to run it, from plain `docker run` to stamping a version into the image, is in [docs/RUN.md](docs/RUN.md).

The API is open unless you ask for authentication. Create the solution with `--include-auth --authority https://login.example.com --audience https://api.example.com` and the movie, author and review endpoints ask for a JWT bearer token. The version endpoint asks for one too. Health checks, the OpenAPI document and Scalar stay open, and Scalar has a spot to paste your token. Leave `--include-auth` out and there's no authentication code in the solution at all, so there's nothing to leave switched off by accident. [Securing the API](https://leanmeancleanapps.github.io/webapi/guides/secure-the-api/) has the details, including how to add it later.

## What's inside

Three layers, loosely based on Clean Architecture. Fewer layers than an onion, more than a sandwich.

- **Presentation** is the HTTP API, with a controller per feature. Each action sends its request through [Mediator](https://github.com/martinothamar/Mediator), and Application errors become [Problem Details](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api) responses.
- **Application** owns the rules. Commands and queries are validated by [FluentValidation](https://github.com/FluentValidation/FluentValidation) and mapped by [Mapperly](https://mapperly.riok.app).
- **Infrastructure** owns the data, with [Entity Framework Core](https://learn.microsoft.com/ef/core) on SQLite. Everything in it is `internal`, so Presentation can wire it up but can't move in.

There's a little CQRS in how commands and queries are shaped, but reads and writes share one database underneath. CQRS-curious, not CQRS-committed.

And around the edges:

- **Tests** use [xUnit](https://xunit.net), [Shouldly](https://github.com/shouldly/shouldly) and [NSubstitute](https://github.com/nsubstitute/NSubstitute), with coverage from [Coverlet](https://github.com/coverlet-coverage/coverlet). Each test gets its own throwaway in-memory database, so no test can spoil another one's data.
- **Sample data** comes from [Bogus](https://github.com/bchavez/Bogus), and only when `Database:SeedSampleData` is on. A real deployment never starts with fake reviews.
- **Telemetry** is [OpenTelemetry](https://opentelemetry.io) all the way: a span for every request and use case, metrics from ASP.NET Core, EF Core and the .NET runtime, and [Serilog](https://github.com/serilog/serilog) logs that know which trace they belong to. It all leaves over OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, so point it at whatever backend you like. Docker Compose runs an [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/) that forwards it to the Aspire dashboard, the same shape you'd run in production. Health checks answer at `/health/live` and `/health/ready`.
- **Build rules** live in `Directory.Build.props`. Code style and the recommended analyzers run on every build, warnings fail it, and package versions sit in one place with [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management).
- **The container** runs as a non-root user and has its own health check.
- **The pipeline** builds, checks formatting, tests with a 95% coverage bar, makes sure the packed template still works, runs [CodeQL](https://codeql.github.com), reviews new dependencies and their licences, starts the image to check it actually answers, and scans the container and Dockerfile with [Trivy](https://trivy.dev). Then it sums the lot up in one comment on your pull request, so you know exactly who to blame. Your copy, or a solution from `dotnet new`, gets the format check, build and tests with no accounts, tokens or secrets, so it goes green the moment you push it. The scans stay switched off outside our organisation until you want them, and Jacques can switch them on with you. Trust is earned.
- **Dependabot** bundles the month's dependency updates into one pull request on the 15th.

The [architecture page](https://leanmeancleanapps.github.io/webapi/explanation/architecture/) has the long version, with full credit to [Jason Taylor's template](https://github.com/jasontaylordev/CleanArchitecture) and [Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) for the ideas.

## Releases

A new version lands on the 1st of each month, as long as something changed, with notes for every pull request in the [GitHub release](https://github.com/LeanMeanCleanApps/CleanWebApi/releases). No changes, no release. We're not here to pad the download stats. Each release also carries an SPDX bill of materials listing every package the API ships with, for anyone whose security team asks nicely (or not so nicely).

```bash
dotnet new update
```

## The family

| Template | Presentation |
| --- | --- |
| [Clean Minimal API](https://github.com/LeanMeanCleanApps/CleanMinimalApi) | ASP.NET Core Minimal APIs |
| [Clean Model Context Protocol](https://github.com/LeanMeanCleanApps/CleanModelContextProtocol) | Model Context Protocol server |
| [Clean GraphQL API](https://github.com/LeanMeanCleanApps/CleanGraphQLApi) | GraphQL with Hot Chocolate |
| **Clean Web API** | ASP.NET Core controllers, you are here |
| [Clean Lambda Function](https://github.com/LeanMeanCleanApps/CleanLambdaFunction) | AWS Lambda |

## Contributing

Bug reports, ideas and pull requests are all welcome, and typo fixes doubly so. Start with the [contributing guide](CONTRIBUTING.md) and the [code of conduct](CODE_OF_CONDUCT.md). Security problems go through the [security policy](SECURITY.md), not an issue.

## Who built this

[<img src="https://github.com/stphnwlsh.png?size=96" width="48" align="left" alt="">](https://github.com/stphnwlsh)
Built by **Stephen Walsh** in spare time, with strong coffee and a healthy disregard for pomp and ceremony. Say hello on [GitHub](https://github.com/stphnwlsh), [LinkedIn](https://www.linkedin.com/in/stphnwlsh), [Medium](https://stphnwlsh.medium.com) or at [stphnwlsh.github.io](https://stphnwlsh.github.io).

If this template saves you some time, [sponsoring the work](https://github.com/sponsors/stphnwlsh) keeps all five templates current and the coffee flowing, which is the real dependency here.

## License

[MIT](LICENSE.md). Take it, change it, ship it. Just don't blame us for your production incidents.
