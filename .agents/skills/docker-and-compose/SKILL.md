---
name: docker-and-compose
description: How the Dockerfile builds, tests and publishes the solution, how the runtime image runs (non-root, /app/data volume, HEALTHCHECK), and what docker-compose starts. Use when changing the Dockerfile, the compose file or how the app runs in a container.
metadata:
  scope: shared
---

# Docker and Compose

## The rule

One multi-stage `Dockerfile` does everything the pipeline needs:

| Stage | Does |
| --- | --- |
| `build` | Restores (project files copied first for layer caching), then builds in Release with the version passed in |
| `test` | Runs every test project with coverage and saves the results and exit code |
| `results` | Exports the test results so the pipeline can report them |
| `publish` | Publishes the Presentation project |
| `run` | The runtime image: non-root `$APP_UID`, `WORKDIR /app`, data in `/app/data`, a `HEALTHCHECK` on `/health/live` |

`docker-compose.yml` runs the app with `DOTNET_ENVIRONMENT=docker`, the `movie-reviews-data` volume and a pinned Seq image for logs. Images are built by CI but not pushed anywhere; the template ships the Dockerfile so users can.

## Steps

1. Keep restore before `COPY . .` so a code change doesn't redo the restore layer. A new project folder needs nothing extra; the loops pick up every `src/*/*.csproj` and `tests/*/*.csproj`.
2. Anything the app writes goes under `/app/data`, the only folder the app user can write to.
3. Pin every image in compose to a version. Dependabot keeps them current.
4. Change base images through the `ARG`s at the top, not inside a `FROM`.
5. Test locally with `docker compose up -d --build`, then hit `/health/ready` and the API reference.

## Don't

- Run the runtime image as root, or write outside `/app/data`.
- Use `:latest` for any image.
- Add a push step to a registry.
- Copy secrets or `appsettings.*.json` overrides with real values into the image.

## Check

``` bash
docker build . --target run --tag local-check
docker compose up -d --build && curl -fsS http://localhost:8080/health/ready
docker compose down
```
