using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Tests;

/// <summary>
/// Helper for running Roslyn analyzers against in-memory source code.
/// Combines TRUSTED_PLATFORM_ASSEMBLIES with already-loaded assemblies to ensure
/// all referenced types (including Microsoft.Extensions.Options etc.) are resolved.
/// </summary>
internal static class AnalyzerTestHelper
{
    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        DiagnosticAnalyzer analyzer,
        string source)
    {
        var references = BuildReferences();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create(analyzer));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static IReadOnlyList<MetadataReference> BuildReferences()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var references = new List<MetadataReference>();

        // 1. Use TRUSTED_PLATFORM_ASSEMBLIES (includes runtime BCL assemblies)
        var trustedPaths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var path in trustedPaths)
        {
            if (seen.Add(path) && File.Exists(path))
                references.Add(MetadataReference.CreateFromFile(path));
        }

        // 2. Add already-loaded assemblies (covers NuGet packages like Microsoft.Extensions.Options
        //    that are referenced by the test project but may not appear in TRUSTED_PLATFORM_ASSEMBLIES)
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
                continue;

            if (seen.Add(assembly.Location) && File.Exists(assembly.Location))
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        return references;
    }
}
