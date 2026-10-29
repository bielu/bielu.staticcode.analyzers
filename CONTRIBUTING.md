# Contributing to Bielu.StaticCode.Analyzers

Thank you for your interest in contributing! Issues and pull requests are welcome.

## Prerequisites

- **.NET SDK 10.0** or later
- **Node.js 22.x** or later (for `npx changeset` — see [Release Process](#release-process))
- **Git**

## Getting Started

```bash
git clone https://github.com/bielu/bielu.staticcode.analyzers.git
cd bielu.staticcode.analyzers
git checkout -b feature/your-feature-name
```

## Building and Testing

```bash
dotnet build src/Bielu.StaticCode.Analyzers.slnx
dotnet test src/Bielu.StaticCode.Analyzers.slnx   # also collects coverage (coverlet.runsettings)
```

To try the package in another project, pack it into `dist/` and add that folder as a local feed:

```bash
dotnet pack src/Bielu.StaticCode.Analyzers/Bielu.StaticCode.Analyzers.csproj -c Release
```

## Adding a Rule

1. Take the next free `BIELU0xx` ID. Shipped IDs are never reused or renumbered.
2. Add the analyzer under `Analyzers/` and, if it can be fixed mechanically, a code fix under `CodeFixProviders/`.
3. Record the rule in `src/Bielu.StaticCode.Analyzers/AnalyzerReleases.Unshipped.md`.
4. Add tests for both the reported and the not-reported cases.
5. Document the rule in the README table and its own section.

## Pull Request Process

1. Make sure the solution builds without new warnings and the tests pass.
2. **Add a changeset** describing your change:
   ```bash
   npx changeset
   ```
   For CI-only, development-tooling or documentation-only changes, record an empty changeset instead:
   ```bash
   npx changeset add --empty
   ```
   The automated **"Version Packages"** PR and Dependabot PRs are exempt; every other PR without a
   changeset fails the `changeset-check` job.
3. Open the pull request against `main`.

## Release Process

Releases are managed through GitHub Actions using [changesets](https://github.com/changesets/changesets)
and follow semantic versioning. Nobody edits `version.props`, `CHANGELOG.md` or
`AnalyzerReleases.Shipped.md` by hand — see [`.changeset/README.md`](.changeset/README.md).

- **Pull requests**: the package is built with a `-pr` suffix and not published.
- **Pushes to `main` while changesets are pending**: a `<pending-version>-beta.<timestamp>` prerelease
  is published to NuGet and surfaced as a GitHub prerelease listing the pending changes.
- **Merging the "Version Packages" PR**: the version committed to `version.props` is published to
  NuGet via trusted publishing, and a matching `vX.Y.Z` GitHub Release is created from that version's
  [`CHANGELOG.md`](./CHANGELOG.md) section.

To see which bump is currently pending:

```bash
npm run changeset:status
```
