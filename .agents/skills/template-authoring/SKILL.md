---
name: template-authoring
description: How the dotnet new template is defined and packaged - .template.config/template.json, sourceName replacement, symbols such as authorName, authority and audience, what template/template.csproj packs, and how to test an install. Use when changing template options, the package contents or anything that affects generated solutions.
metadata:
  scope: maintainer
---

# Template authoring

## The rule

The repository is both a working solution and a `dotnet new` template. `.template.config/template.json` defines the template; `template/template.csproj` packs it into a NuGet package of type `Template`. When someone runs `dotnet new`, every occurrence of the `sourceName` (the template's own name, such as `CleanWebApi`) in file names and contents is replaced with their project name.

| Symbol | Option | Replaces |
| --- | --- | --- |
| `authorName` | `--authorName` | `Stephen Walsh` (`--author` is reserved by `dotnet new`) |
| `authority`, `audience` | `--authority`, `--audience` | The empty `Authentication` settings in `appsettings.json` (web templates) |

The package includes `src/`, `tests/`, `docs/RUN.md`, the root build files, `AGENTS.md`, `CLAUDE.md`, the persona and the engineering skills. It leaves out `.github/`, the community files and the repository and maintainer skills, which only make sense in the template repository. Those skills are listed in `RepositorySkills` in `template/template.csproj`; a new one is added there. The pipeline fails a package over 1 MB.

## Steps

1. Change `template.json` or `template.csproj`.
2. Keep the `sourceName` out of identifiers. A class called `CleanWebApiApplication` becomes invalid when the name is replaced with something like `My.Api`.
3. Test the install from the repository root:

   ``` bash
   dotnet new install ./ --force
   dotnet new <shortName> --name Sample --output ../Sample
   dotnet build ../Sample && dotnet test ../Sample
   dotnet new uninstall ./
   ```

4. For package contents, also pack and look inside:

   ``` bash
   dotnet pack template/template.csproj --output packages
   unzip -l packages/*.nupkg
   ```

5. A new option goes in the README and `NUGET.md` options tables.

## Don't

- Change `identity` or `shortName`; existing installs and docs depend on them.
- Pack with a wildcard over the whole repository; it pulls in build output and pipeline files.
- Add an option without testing the generated solution with it on and off.

## Check

The Test job's Verify Template step installs the packed template, generates a solution and runs its tests.
