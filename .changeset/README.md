# Changesets

This folder is managed by [changesets](https://github.com/changesets/changesets). Every pull
request that changes the shipped package should include a **changeset** — a small markdown file that
records what changed and how the version should bump. CI aggregates these into an automated
"Version Packages" PR that bumps the version and updates the changelog.

## Add a changeset

```bash
npx changeset
```

Pick the `bielu-staticcode-analyzers` entry and a bump level, then write a short, user-facing
summary. It stands in for the `Bielu.StaticCode.Analyzers` NuGet package, whose version lives in
`version.props`.

Bump levels follow semver. For an analyzer package, a new rule or a stricter default severity changes
the build output of every consumer, so treat it as a feature rather than a fix:

- **patch** — bug fixes (false positives/negatives, code fix corrections), docs, internal changes.
- **minor** — new rules, new code fixes, backward-compatible options.
- **major** — removed or renumbered rules, or severity changes that turn existing code into errors.

Chore-only PRs (CI, formatting, tests) can record an empty changeset:

```bash
npx changeset add --empty
```

## How the NuGet version is applied

Changesets is npm-native, so the NuGet package is represented by a private placeholder package,
`bielu-staticcode-analyzers` (in `build/changeset/nuget-suite`). After `changeset version` runs,
`scripts/apply-nuget-version.mjs` copies that package's new version into `version.props`
`<VersionPrefix>` and splices its generated changelog section into the root `CHANGELOG.md`, and
`scripts/ship-analyzer-releases.mjs` moves the rules in `AnalyzerReleases.Unshipped.md` into a
`## Release x.y.z` section of `AnalyzerReleases.Shipped.md`.
