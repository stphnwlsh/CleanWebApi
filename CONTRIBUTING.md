# Contributing to Clean Web API

Thanks for stopping by. Bug reports, ideas, documentation fixes and pull requests are all welcome, roughly in that order of how quickly they get a reply.

This is a [Lean Mean Clean Apps](https://leanmeancleanapps.github.io/) project, built and looked after in spare time with strong coffee and a healthy disregard for pomp and ceremony. The team is one person at the moment, so replies are best effort, and the fastest way to get something changed is to make it easy to say yes to. If you'd like to make it two people, pull requests are how that starts.

## Before you start

- Be kind. The [code of conduct](CODE_OF_CONDUCT.md) says how.
- Search the [existing issues](https://github.com/LeanMeanCleanApps/CleanWebApi/issues) first. Someone may have beaten you to it.
- For anything larger than a small fix, open an issue first, so we can agree on the approach before you spend an evening on it.
- Report security vulnerabilities privately, as described in the [security policy](SECURITY.md), not in a public issue.

## One template in a family of five

This repository is one of five [Lean Mean Clean Apps](https://leanmeancleanapps.github.io/) templates that share everything except the Presentation layer:

- [Clean Minimal API](https://github.com/LeanMeanCleanApps/CleanMinimalApi)
- [Clean Model Context Protocol](https://github.com/LeanMeanCleanApps/CleanModelContextProtocol)
- [Clean GraphQL API](https://github.com/LeanMeanCleanApps/CleanGraphQLApi)
- [Clean Web API](https://github.com/LeanMeanCleanApps/CleanWebApi)
- [Clean Lambda Function](https://github.com/LeanMeanCleanApps/CleanLambdaFunction)

If your change touches the Application or Infrastructure layers, their tests, the build pipeline or the repository setup, it needs to be made in all five. Mention that in your pull request and the maintainer will carry it across. You fix it once, the family gets it five times.

## Development setup

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Docker is optional but needed to build the container image.
1. Fork the repository and clone your fork.
1. Build and run the tests.

   ``` bash
   dotnet build
   dotnet test
   ```

1. Run the project as described in [docs/RUN.md](docs/RUN.md).
1. The docs site lives in the [Lean Mean Clean Apps site repository](https://github.com/LeanMeanCleanApps/leanmeancleanapps.github.io), so send docs changes there.

### Testing the template itself

To check that the template still installs and produces a working solution, install it from your local clone and create a solution from it.

``` bash
dotnet new install ./ --force
dotnet new cleanwebapi --name Sample --output ../Sample
dotnet build ../Sample
dotnet new uninstall ./
```

## Making a change

1. Create a branch from `main` named `feature/`, `fix/` or `chore/` followed by a short description, for example `fix/review-validation`. The repository rejects any other branch name (Dependabot is the one exception), and the prefix decides the release notes label: `feature/` is a new feature, `fix/` is a bug fix, and `chore/` lands under other changes.
1. Keep the change focused. Unrelated refactors are easier to review as separate pull requests.
1. Add or update tests. The pipeline fails if line coverage drops below 95%. It doesn't negotiate, and neither does the maintainer.
1. Follow the style in `.editorconfig` and `Directory.Build.props`. Warnings are treated as errors, so the build must be clean. A warning is just an error that hasn't been caught yet.
1. Update the README or docs if behaviour changes.

## Working with an AI agent

Agents are welcome here, as long as a person stays in charge. [AGENTS.md](AGENTS.md) is the agent's map of the repository, [Jacques](.agents/agents/jacques.md), our sous-chef, is the persona they work as, and [.agents/skills](.agents/skills) holds one skill per kind of change, in the open Agent Skills format that most coding agents understand. If you change how something is done, update its skill in the same pull request. Out-of-date recipes are how kitchens catch fire.

## Pull requests

1. Open the pull request against `main` and fill in the template.
1. Link the issue it resolves, for example `Closes #123`.
1. Every check must pass: Build, Test, Static Analysis, Dependency Review, Container Scan and Summary. The Summary comment on your pull request says which one is unhappy and links to the details.
1. The maintainer reviews every pull request and squash-merges it once it's green. That's not a lack of trust, it's just how the lights stay on. If a review takes a while, a polite nudge is fine.

## Getting help

See [SUPPORT.md](SUPPORT.md).
