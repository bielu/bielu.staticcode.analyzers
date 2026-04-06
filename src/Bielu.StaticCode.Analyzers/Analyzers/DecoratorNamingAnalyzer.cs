using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that enforces the decorator naming convention.
/// Decorator classes implementing IFoo with an IFoo constructor parameter must be named {Modifier}FooDecorator.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DecoratorNamingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Decorator naming convention violation",
        messageFormat: "Decorator class '{0}' implementing '{1}' should be named '{{Modifier}}{2}Decorator'",
        category: "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Decorator classes should follow the pattern: {Modifier}{InterfaceName}Decorator. " +
                     "For example, a class decorating IApiService should be named CachedApiServiceDecorator.");

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

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol is null || classSymbol.IsAbstract || classSymbol.IsStatic)
            return;

        // Find interfaces directly implemented by this class that follow the I{Name} convention
        var implementedInterfaces = classSymbol.Interfaces
            .Where(i => i.Name.Length > 1 && i.Name[0] == 'I' && char.IsUpper(i.Name[1]))
            .ToList();

        if (implementedInterfaces.Count == 0)
            return;

        // Collect all constructor parameters (regular constructors + primary constructor)
        var constructorParameters = classDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .SelectMany(c => c.ParameterList.Parameters)
            .Concat(
                classDeclaration.ParameterList?.Parameters
                    ?? Enumerable.Empty<ParameterSyntax>())
            .ToList();

        foreach (var iface in implementedInterfaces)
        {
            // A class is a decorator if it has a constructor parameter of the same interface type
            var isDecorator = constructorParameters.Any(p =>
            {
                if (p.Type is null)
                    return false;

                var paramType = context.SemanticModel.GetTypeInfo(p.Type).Type;
                return paramType is not null &&
                       SymbolEqualityComparer.Default.Equals(paramType, iface);
            });

            if (!isDecorator)
                continue;

            // Interface name without the 'I' prefix (e.g. IApiService -> ApiService)
            var nameWithoutPrefix = iface.Name.Substring(1);
            var expectedSuffix = $"{nameWithoutPrefix}Decorator";

            if (!classDeclaration.Identifier.Text.EndsWith(expectedSuffix, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    classDeclaration.Identifier.GetLocation(),
                    classDeclaration.Identifier.Text,
                    iface.Name,
                    nameWithoutPrefix));
            }
        }
    }
}
