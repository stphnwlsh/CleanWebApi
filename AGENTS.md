# Agent instructions

You're working in a .NET 10 solution built with a streamlined Clean Architecture. This file is the map. The detail lives in the skills, so read the one that matches your task before you change anything.

Start with [Jacques](.agents/agents/jacques.md), our sous-chef and the persona every agent here works as. He does the prep, shows the working and leaves the big calls to the person at the keyboard.

## What's here

| Path | What it is |
| --- | --- |
| `src/Application` | Use cases: commands, queries, handlers, validators, entities and the repository interfaces. No framework or database code. |
| `src/Infrastructure` | EF Core with SQLite, the repository, migrations, sample data and the mapping between database models and Application entities. |
| `src/Presentation` | The Web API: controllers, request models, error handling, OpenAPI with Scalar, authentication, health checks and startup. |
| `tests/Application.Tests.Unit` | Handler and validator tests with NSubstitute. |
| `tests/Infrastructure.Tests.Integration` | Repository tests against a private in-memory SQLite database. |
| `tests/Presentation.Tests.Unit` | Error handler, settings, OpenAPI transformer and JSON options tests. |
| `tests/Presentation.Tests.Integration` | Real HTTP requests through `WebApplicationFactory`. |
| `.agents/skills` | How we do each kind of change, one skill per folder. |

Application and Infrastructure, and their tests, are identical in the five Lean Mean Clean Apps templates. Only Presentation differs.

## Commands

Run these before you say a change is done. They're what CI runs.

``` bash
dotnet build                                   # warnings are errors
dotnet test                                    # every test project
dotnet format --verify-no-changes              # style, whitespace and usings
dotnet run --project ./src/Presentation        # the API at http://localhost:8080/scalar
```

The `coverage` skill has the command that checks the 95% line coverage gate locally.

## Where we track work

Not set. Replace this line with where your team plans work, such as a GitHub project, a Jira board or Azure Boards, so agents put their plans and handoffs there. In the template repository itself, it's issues in [LeanMeanCleanApps/.github](https://github.com/LeanMeanCleanApps/.github/issues).

## Rules that fail a change

- The build has zero warnings. `TreatWarningsAsErrors`, `latest-recommended` analysers and `.editorconfig` style are all enforced by the build.
- Line coverage stays at or above 95%.
- The house style holds: `this.` on members, `using` directives inside the file-scoped namespace, `_ =` for unused results, `#region` blocks for long classes. See `csharp-house-style`.
- Dependencies point inwards: Presentation and Infrastructure depend on Application, never the other way. See `clean-architecture-layers`.
- Package versions live in `Directory.Packages.props`, never in a project file.

## Never

- Push to `main`, or merge a pull request.
- Skip, delete or weaken a test, an analyser or the coverage gate to get a green build.
- Suppress a warning instead of fixing it, unless a skill says that exact suppression is ours.
- Add a package with a commercial or paid licence, such as MediatR, MassTransit or AutoMapper.
- Commit secrets, connection strings with passwords, or tokens.
- Change `.template.config/` or the template package without following `template-authoring`.

## Skills

Each skill is a folder in `.agents/skills` with a `SKILL.md`. If your tool doesn't load skills by itself, open the file by name.

| Skill | Use it when you |
| --- | --- |
| `clean-architecture-layers` | Decide where new code belongs |
| `add-a-use-case` | Add a command or query with its handler and validator |
| `mediator` | Add a pipeline behaviour or change how requests are sent |
| `validation` | Add or change validation rules |
| `errors-and-exceptions` | Report not found or invalid input |
| `paging` | Return a list |
| `data-access-ef-core` | Read or write the database |
| `database-migrations` | Change the data model |
| `sample-data` | Change the seed data or test data |
| `mapping-with-mapperly` | Map between database models and entities |
| `cancellation` | Pass a cancellation token anywhere |
| `time-provider` | Need the current date or time |
| `configuration-and-environments` | Add a setting or work with environments |
| `serilog` | Log something or change log settings |
| `telemetry` | Add a span or metric, or change where traces, metrics and logs go |
| `health-checks` | Change what liveness or readiness checks |
| `optional-token-auth` | Protect an endpoint or change authentication |
| `xml-docs-and-openapi` | Document a public type, endpoint or model |
| `csharp-house-style` | Write any C# |
| `packages-and-dependencies` | Add, update or remove a package |
| `docker-and-compose` | Change the Dockerfile or compose file |
| `test-design` | Decide which tests a change needs |
| `unit-tests` | Test a handler, validator or endpoint |
| `integration-tests` | Test the repository or a real HTTP request |
| `coverage` | Check or fix line coverage |
| `writing-a-skill` | Add or change a skill, or build a feature with a new pattern |

These are for this Web API's presentation layer. The other templates have their own, with the same names where the job is the same:

| Skill | Use it when you |
| --- | --- |
| `expose-a-use-case` | Put a use case on a controller action |
| `api-conventions` | Choose a route, status code or response shape |
| `json-serialization` | Add a request or response type, or change how JSON looks |
| `openapi-and-scalar` | Change the API description, security scheme or Scalar |
| `presentation-integration-tests` | Write a test that makes a real HTTP request |

These are for working on the template repository itself, and they aren't included in solutions created with `dotnet new`:

| Skill | Use it when you |
| --- | --- |
| `branches-and-pull-requests` | Name a branch or open a pull request |
| `ci-pipeline` | Read or fix a pipeline check |
| `security-findings` | Deal with a CodeQL, Trivy or dependency finding |
| `docs-and-voice` | Write a README, docs page or release note |
| `template-authoring` | Change `.template.config/` or the package |
| `family-sync` | Carry a shared change to all five templates |
| `releases` | Work with the monthly release |

## Done means

- [ ] The matching skill was followed, or updated in the same change if the way we do things changed.
- [ ] `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` pass.
- [ ] New behaviour has tests for every success and failure (`test-design`), and coverage stays at or above 95%.
- [ ] A feature that brings a new pattern has a skill for it (`writing-a-skill`).
- [ ] Public types and endpoints have XML comments.
- [ ] Docs are updated where behaviour changed.
