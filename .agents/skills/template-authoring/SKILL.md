---
name: template-authoring
description: How the dotnet new template is defined and packaged - .template.config/template.json and its two host files, sourceName replacement, symbols such as authorName, authority and audience, short flags, the restore post action, what template/template.csproj packs, and how to test an install. Use when changing template options, the package contents or anything that affects generated solutions.
metadata:
  scope: maintainer
---

# Template authoring

## The rule

The repository is both a working solution and a `dotnet new` template. `.template.config/template.json` defines the template; `template/template.csproj` packs it into a NuGet package of type `Template`. When someone runs `dotnet new`, every occurrence of the `sourceName` (the template's own name, such as `CleanMinimalApi`) in file names and contents is replaced with their project name, including `CleanMinimalApi.slnx`.

| Symbol | Option | Replaces |
| --- | --- | --- |
| `authorName` | `--authorName`, `-an` | `Stephen Walsh` (`--author` is reserved by `dotnet new`) |
| `authority`, `audience` | `--authority`, `-iss`, `--audience`, `-aud` | The empty `Authentication` settings in `appsettings.json` (web templates) |
| `skipRestore` | `--no-restore` | Nothing; it turns the restore post action off |

Three files in `.template.config` make up the template, and every symbol appears in each one that applies:

| File | Used by | Holds |
| --- | --- | --- |
| `template.json` | Every host | Identity, `groupIdentity`, `precedence` and `generatorVersions`, `tags.type` of `solution`, the standard classifications, `defaultName`, an SDK `constraints` entry matching `global.json`, the symbols with a `displayName` and a `description`, a `HostIdentifier` bind, `primaryOutputs` (the `.slnx`, and `docs/RUN.md` for IDEs only) and three post actions: restore, open `docs/RUN.md` in the IDE, and a next-steps line in the terminal |
| `dotnetcli.host.json` | `dotnet new` | Each symbol's `longName` and short flag, and `usageExamples` (kept for hosts that show them; the .NET 10 `--help` doesn't) |
| `ide.host.json` | Visual Studio, Rider | The icon (`.template.config/icon.png`, a copy of the root `icon.png`) and the label, description and visibility of each symbol in the new project dialog |

The words in those files are the first thing people read, in a terminal or a dialog. Write them in our voice (`docs-and-voice`): plain first, then one dry line if it earns its place. The `--help` text keeps to plain humour. The one nod to a certain long-running .NET podcast lives in the author name help in `ide.host.json`, with its twin on the docs site's getting started pages. Keep it to those two, and never explain the joke. The `defaultName` is `WorksOnMyMachine.Movies` (`WorksOnMyMachine.Reviews` in the Lambda function). Visual Studio and Rider suggest it as the project name, and `dotnet new` uses the folder name instead, so the joke only reaches people who take the IDE's suggestion.

The package includes `src/`, `tests/`, `docs/RUN.md`, the root build files, `AGENTS.md`, `CLAUDE.md`, the persona and the engineering skills. It leaves out `.github/`, the community files and the repository and maintainer skills, which only make sense in the template repository. Those skills are listed in `RepositorySkills` in `template/template.csproj`; a new one is added there. The pipeline fails a package over 1 MB.

## Steps

1. Change `template.json` or `template.csproj`. `template.csproj` references `Microsoft.TemplateEngine.Authoring.Tasks`, Microsoft's template validator, which checks `.template.config` on every build and pack; keep its output free of messages, not just errors. A new option also goes in `dotnetcli.host.json` with a short flag that doesn't clash with `dotnet new`'s own (`-n`, `-o`, `-lang`, `-h`), and in `ide.host.json` with `isVisible` set, or Visual Studio hides it.
2. Keep the `sourceName` out of identifiers. A class called `CleanMinimalApiApplication` becomes invalid when the name is replaced with something like `My.Api`.
3. Test the install from the repository root:

   ``` bash
   dotnet new install ./ --force
   dotnet new <shortName> --help
   dotnet new <shortName> --name Sample --output ../Sample
   dotnet build ../Sample && dotnet test ../Sample
   dotnet new uninstall ./
   ```

   `--help` shows the short flags and descriptions. Creating the solution should end with `Restoring .../Sample.slnx` and `Restore succeeded`, which is the post action at work; `--no-restore` should skip it.

4. For package contents, also pack and look inside:

   ``` bash
   dotnet pack template/template.csproj --output packages
   unzip -l packages/*.nupkg
   ```

5. A new option goes in the README and `NUGET.md` options tables, and on the docs site's template options page.

## Don't

- Change `identity` or `shortName`; existing installs and docs depend on them.
- Make a primary output that isn't a project visible to the CLI. The restore post action restores every primary output it can see, so `docs/RUN.md` carries the same `HostIdentifier` condition as the action that opens it.
- Add a `.sln`. The solution is `.slnx` only, and `primaryOutputs`, the Dockerfile and the package all point at it.
- Reuse a short flag across options, or give one to `--no-restore`; the `dotnet new` templates from Microsoft leave it long, and so do we.
- Pack with a wildcard over the whole repository; it pulls in build output and pipeline files.
- Add an option without testing the generated solution with it on and off.

## Check

The Test job's Verify Template step installs the packed template, generates a solution and runs its tests.
