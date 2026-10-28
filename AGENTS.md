# AGENTS.md

Guidance for AI coding agents working in this repository. Human contributors should also read [CONTRIBUTING.md](./CONTRIBUTING.md).

## What this project is

`Bielu.StaticCode.Analyzers` is a NuGet package of **Roslyn analyzers and code fixes** enforcing the coding conventions used across the bielu .NET ecosystem (decorator/wrapper naming, primary constructors, `IOptionsMonitor<T>`, `ILogger<T>`, `ConfigureAwait(false)`, guard clauses, sealed internal classes…). Other bielu repositories reference it with `PrivateAssets="all"`, so every rule change shows up in their builds. The rule catalogue lives in [README.md](./README.md).

## Repository layout

The solution is `src/Bielu.StaticCode.Analyzers.slnx`. Everything, tests included, lives under `src/`.

| Path | Purpose |
|------|---------|
| `src/Bielu.StaticCode.Analyzers/Analyzers/` | One `DiagnosticAnalyzer` per rule (`BIELU0xx`) |
| `src/Bielu.StaticCode.Analyzers/CodeFixProviders/` | Code fixes, named after the analyzer they fix |
| `src/Bielu.StaticCode.Analyzers/AnalyzerReleases.*.md` | Release tracking for rule IDs (RS2000–RS2008) |
| `src/Bielu.StaticCode.Analyzers.Tests/` | xUnit tests (not packed); `AnalyzerTestHelper` compiles in-memory sources |
| `assets/` | Package icon (`icon.svg`, rendered to the packed `icon.png`) |
| `scripts/` | Release tooling run by CI and `npm run version` |

## Conventions

- **Target framework:** the analyzer targets `netstandard2.0` (required for analyzers loaded by the compiler); tests target `net10.0`. Requires the **.NET 10 SDK**.
- **Roslyn floor:** `Microsoft.CodeAnalysis.CSharp(.Workspaces)` in [`Directory.Packages.props`](./Directory.Packages.props) is the minimum compiler consumers need. Do not bump it for the sake of being current; an analyzer built against a newer Roslyn than the consumer's SDK fails to load (CS8032).
- **Central package management:** all package versions live in `Directory.Packages.props`. Reference packages with `<PackageReference Include="..." />` and **no `Version` attribute**.
- **Shared MSBuild properties:** [`src/Directory.Build.props`](./src/Directory.Build.props) (package metadata, license, Source Link, icon/readme packing, coverage settings). **Version** is centralized in [`version.props`](./version.props) and written by the release tooling — never edit it or hardcode versions in csproj files.
- **New rule:** take the next free `BIELU0xx` ID, add the analyzer (and optional code fix), add a row to `AnalyzerReleases.Unshipped.md` and a section to the README table. Never reuse or renumber a shipped ID.
- **Formatting:** governed by [`.editorconfig`](./.editorconfig) (4-space indent for C#, final newline). File-scoped namespaces.

## Engineering principles

- **SOLID** — one rule per analyzer, one code fix per provider; shared syntax helpers over copy-pasted logic.
- **DRY** — reuse existing helpers and test utilities (`AnalyzerTestHelper`); shared MSBuild/package config already lives in `Directory.*.props` and `version.props`.
- **KISS** — prefer the simplest detection that is correct; avoid false positives over catching every edge case, since a noisy rule gets suppressed wholesale.
- **Performance** — analyzers run on every keystroke in the IDE: register the narrowest syntax/symbol action, enable concurrent execution and skip generated code.

## Code coverage

Target **70%+ line coverage** for code you add or change. [`coverlet.runsettings`](./coverlet.runsettings), applied to every test project via `src/Directory.Build.props`, turns the `coverlet.collector` on for every `dotnet test` run and measures only the `Bielu.*` library assemblies. Cobertura reports land under the test project's `TestResults/`.

## Build, test, format

```bash
dotnet build src/Bielu.StaticCode.Analyzers.slnx
dotnet test src/Bielu.StaticCode.Analyzers.slnx
dotnet format --verify-no-changes src/Bielu.StaticCode.Analyzers.slnx
```

## Testing conventions

- **xUnit** + **Shouldly** assertions.
- Every rule gets tests for code that must be reported **and** code that must not (the false-positive cases matter as much).
- Name tests `Scenario_ShouldReportDiagnostic` / `Scenario_ShouldNotReportDiagnostic` (code fixes: `Scenario_ShouldFix...`), matching the existing suite.

## Versioning and releases

Releases are driven by [changesets](https://github.com/changesets/changesets) (see [`.changeset/README.md`](./.changeset/README.md)). Every PR that changes the package adds a changeset bumping `bielu-staticcode-analyzers` (`npx changeset`); chore-only PRs use `npx changeset add --empty`. CI enforces this (`changeset-check`), except on Dependabot and Version Packages PRs. Merging to `main` publishes a beta when shipped code changed; merging the Version Packages PR publishes the stable release via nuget.org trusted publishing and moves unshipped rules into `AnalyzerReleases.Shipped.md`.

## Before opening a PR

1. `dotnet build` succeeds with no new warnings.
2. `dotnet test` passes.
3. `dotnet format --verify-no-changes src/Bielu.StaticCode.Analyzers.slnx` reports nothing new.
4. A changeset is included.
5. `README.md` and `AnalyzerReleases.Unshipped.md` are updated when rules change.
6. Target the `main` branch.
