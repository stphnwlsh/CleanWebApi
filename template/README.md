# CleanWebApi

Built on .NET 10 with Clean Architecture: `Application` owns the domain rules and use cases, `Infrastructure` owns Entity Framework Core and data access, and `Presentation` is the entry point.

## Build and test

```bash
dotnet build
dotnet test
```

## Run locally

```bash
docker compose up -d --build
```

See [docs/RUN.md](docs/RUN.md) for the local ports, `dotnet run`, the Aspire dashboard, database migrations and health checks.

## Docs and coding agents

- [AGENTS.md](AGENTS.md): architecture rules, project map and the skills in `.agents/skills/` for coding agents.
- [Template documentation](https://leanmeancleanapps.github.io/webapi/): guides, reference pages and design decisions.
