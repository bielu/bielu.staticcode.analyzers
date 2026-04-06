using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that recommends the <c>sealed</c> modifier on <c>internal</c> classes.
/// Sealing internal classes prevents accidental inheritance and enables runtime optimizations
/// (devirtualization). This is a common pattern in the bielu ecosystem for implementation classes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SealedInternalClassAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU009";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Internal class should be sealed",
        messageFormat: "Internal class '{0}' should be sealed to prevent unintended inheritance and enable runtime optimizations",
        category: "Design",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Internal classes that are not designed for inheritance should be marked as sealed. " +
                     "This prevents accidental inheritance, makes intent clear, and allows the JIT " +
                     "compiler to apply devirtualization optimizations.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Only target internal classes (default accessibility or explicit internal)
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol is null)
            return;

        // Skip if already sealed, abstract, or static
        if (classSymbol.IsSealed || classSymbol.IsAbstract || classSymbol.IsStatic)
            return;

        // Only target internal (not public, protected, etc.)
        if (classSymbol.DeclaredAccessibility != Accessibility.Internal)
            return;

        // Skip if the class has any derived types in the same assembly
        // (we can only check syntax-level within the compilation)
        if (HasDerivedTypes(context, classSymbol))
            return;

        // Skip if the class has any virtual or abstract members (intended for inheritance)
        if (HasVirtualOrAbstractMembers(classSymbol))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            classDeclaration.Identifier.GetLocation(),
            classDeclaration.Identifier.Text));
    }

    private static bool HasDerivedTypes(SyntaxNodeAnalysisContext context, INamedTypeSymbol classSymbol)
    {
        // Check all class declarations in the same syntax tree for base types.
        // This is a conservative check — only looks within the current file to avoid
        // invoking Compilation.GetSemanticModel() which is prohibited in analyzers (RS1030).
        var root = context.Node.SyntaxTree.GetRoot(context.CancellationToken);

        foreach (var classNode in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(classNode);
            if (symbol?.BaseType is not null &&
                SymbolEqualityComparer.Default.Equals(symbol.BaseType, classSymbol))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasVirtualOrAbstractMembers(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetMembers().Any(member =>
            member is IMethodSymbol or IPropertySymbol or IEventSymbol &&
            (member.IsVirtual || member.IsAbstract));
    }
}
