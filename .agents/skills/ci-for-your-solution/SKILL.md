---
name: ci-for-your-solution
description: How to build a CI pipeline for a solution created with dotnet new, which ships without one - the checks to run, the local commands they must match, what to leave out, and the template's Set up CI guide as the reference. Use when the solution has no pipeline yet, when adding a check to one, or when the engineer asks how to set up CI on GitHub Actions, Azure Pipelines, GitLab or anything else.
metadata:
  scope: shared
---

# CI for your solution

## The rule

A solution created with `dotnet new` has no pipeline, because every team runs a different CI system. Build one with the engineer, on the system they use, that runs the same checks as the commands in `AGENTS.md`. Then the pipeline and a laptop always agree on what "green" means.

The reference is the **Set up CI** guide for your template on the [Lean Mean Clean Apps docs site](https://leanmeancleanapps.github.io/). It lists these checks and links the template's own Build Pipeline, a GitHub Actions workflow that runs every check below on every pull request. Read them for the details, then build the engineer's own version. Don't copy the Build Pipeline as it is. It also packs and publishes the template, which a solution doesn't need.

| Check | Local command | Fails when |
| --- | --- | --- |
| Format | `dotnet format --verify-no-changes` | Style, whitespace or `using` order is off |
| Build | `dotnet build` | Any warning or error. `Directory.Build.props` already treats warnings as errors |
| Test and coverage | `dotnet test`, with the coverage settings in `coverage`, then a check of the merged total | A test fails, or line coverage drops under 95% |
| Vulnerable packages | `dotnet list package --vulnerable --include-transitive` | A package has a known high or critical vulnerability. The command reports but always exits 0, so fail the step when its output contains `High` or `Critical` |
| Container | `docker build . --target run` | The image doesn't build. Add an image scanner such as Trivy if the team ships the container |
| Static analysis | The CI system's own scanner, such as CodeQL on GitHub | A security finding the team hasn't accepted |

## Steps

1. **Ask which CI system.** If the code is on GitHub, recommend GitHub Actions, because the reference already uses it. Otherwise use what the team has: Azure Pipelines, GitLab CI, Jenkins, anything that runs a shell.
2. **Agree the checks.** Recommend starting with Format, Build and Test and coverage, because they need nothing but the .NET SDK. Add the rest as separate steps once those are green. Ask about each one the team may not want, such as the container check for a team that doesn't ship the image.
3. **Write it.** One pipeline that runs on every pull request and every push to the main branch. Use the system's standard tasks or actions, run the exact local commands from the table, and keep the coverage settings in step with `coverage`.
4. **Prove it.** Open a pull request with the pipeline so it runs on itself. Then break something on purpose, like a formatting change or a failing assertion, and check the pipeline goes red before you put it back.
5. **Hand over.** Tell the engineer which checks to make required on the main branch. That's their setting, not yours.

## Example

Format, build, and test with coverage on GitHub Actions. Coverlet merges every test project into one report, so the 95% gate checks the merged total once at the end. A per-project Coverlet threshold would judge each project on a partly merged report.

``` yaml
- uses: actions/setup-dotnet@v6
  with:
    global-json-file: global.json
- run: dotnet format --verify-no-changes
- run: dotnet build --configuration Release
- run: >-
    dotnet test --configuration Release --no-build -m:1
    -p:CollectCoverage=true -p:CoverletOutput=../results/ -p:MergeWith="../results/coverage.json"
    -p:CoverletOutputFormat=opencover%2cjson
    -p:ExcludeByFile="**/Program.cs%2c**/Migrations/*.cs%2c**/obj/**/*.cs"
    -p:ExcludeByAttribute="GeneratedCodeAttribute"
- name: Coverage gate
  run: |
    coverage=$(grep -o -m1 'sequenceCoverage="[0-9.]*"' tests/results/coverage.opencover.xml | grep -o '[0-9.]*')
    echo "Line coverage: $coverage%"
    awk -v c="$coverage" 'BEGIN { exit (c < 95) }'
```

## Don't

- Copy the template-only jobs from the reference: packing and verifying the template, Release Check and Publish.
- Make a check depend on a secret, a paid service or an account to pass on a pull request.
- Lower the coverage threshold or skip a test to get the first run green. Fix the cause (`coverage`, `test-design`).
- Run different commands in the pipeline from the ones in `AGENTS.md`. If one has to change, change both.
- Set up the CI system, runners or branch rules yourself. Write the pipeline and say what the engineer needs to switch on.

## Check

- The pipeline runs on a pull request and every check is green.
- A deliberate formatting change or failing test turns it red.
- `dotnet format --verify-no-changes`, `dotnet build` and `dotnet test` pass locally on the same commit.
