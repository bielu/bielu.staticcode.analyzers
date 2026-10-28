---
"bielu-staticcode-analyzers": patch
---

Align the repository with the bielu ecosystem standards: changeset-driven versioning, NuGet trusted publishing from this repository's own workflow, a daily ClearlyDefined harvest, `Copyright` and Source Link repository metadata in the package, and no empty dependency group in the nuspec. Test and build tooling updated to the latest releases (`Microsoft.CodeAnalysis.Analyzers` 5.9.0, `Microsoft.NET.Test.Sdk` 18.11.0, `xunit.runner.visualstudio` 4.0.1, `coverlet.collector` 10.1.0, `Microsoft.Extensions.*` 10.0.14 in tests); the Roslyn API floor stays at 5.3.0 so consumers on older .NET 10 SDKs keep loading the analyzers.
