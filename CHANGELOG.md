# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
`[Unreleased]` section and released version entries are managed with
[changesets](https://github.com/changesets/changesets) — see [`.changeset/README.md`](.changeset/README.md).

> **Versioning note.** No stable release has shipped yet. `Bielu.StaticCode.Analyzers` has so far only
> been published to the **`beta` pre-release channel** on NuGet.org (`1.0.0-beta.*`). The bootstrap
> changeset versions the first stable release as `1.0.0`, shipping rules `BIELU001`–`BIELU012`.

## [Unreleased]

## [1.0.0] - 2026-10-29

### Major Changes

- [#36](https://github.com/bielu/bielu.staticcode.analyzers/pull/36) [`e7e2b46`](https://github.com/bielu/bielu.staticcode.analyzers/commit/e7e2b466ac99a15dae5054e6076db2b91386efbe) Thanks [@bielu](https://github.com/bielu)! - First stable release, shipping rules `BIELU001`–`BIELU012` (previously published only as `1.0.0-beta.*` prereleases).

### Patch Changes

- [#36](https://github.com/bielu/bielu.staticcode.analyzers/pull/36) [`e7e2b46`](https://github.com/bielu/bielu.staticcode.analyzers/commit/e7e2b466ac99a15dae5054e6076db2b91386efbe) Thanks [@bielu](https://github.com/bielu)! - Align the repository with the bielu ecosystem standards: changeset-driven versioning, NuGet trusted publishing from this repository's own workflow, a daily ClearlyDefined harvest, `Copyright` and Source Link repository metadata in the package, and no empty dependency group in the nuspec. Test and build tooling updated to the latest releases (`Microsoft.CodeAnalysis.Analyzers` 5.9.0, `Microsoft.NET.Test.Sdk` 18.11.0, `xunit.runner.visualstudio` 4.0.1, `coverlet.collector` 10.1.0, `Microsoft.Extensions.*` 10.0.14 in tests); the Roslyn API floor stays at 5.3.0 so consumers on older .NET 10 SDKs keep loading the analyzers.

