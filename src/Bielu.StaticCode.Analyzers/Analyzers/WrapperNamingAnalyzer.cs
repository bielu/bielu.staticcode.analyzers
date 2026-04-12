using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that enforces the wrapper naming convention.
/// A wrapper class accepts a concrete class as a constructor parameter and exposes similar methods.
/// Unlike a decorator (which implements the same interface), a wrapper wraps a concrete type directly.
/// Such classes must be named {Modifier}{ClassName}Wrapper.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WrapperNamingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU010";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Wrapper naming convention violation",
        messageFormat: "Wrapper class '{0}' wrapping '{1}' should be named '{{Modifier}}{1}Wrapper'",
        category: "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Wrapper classes should follow the pattern: {Modifier}{ClassName}Wrapper. " +
                     "For example, a class wrapping HttpClient should be named RetryHttpClientWrapper.");

    private static readonly HashSet<string> ObjectMethodNames = new(StringComparer.Ordinal)
    {
        "ToString", "GetHashCode", "Equals", "GetType", "Finalize", "MemberwiseClone", "Dispose"
    };

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

        // Collect all constructor parameters (regular constructors + primary constructor)
        var constructorParameters = classDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .SelectMany(c => c.ParameterList.Parameters)
            .Concat(
                classDeclaration.ParameterList?.Parameters
                    ?? Enumerable.Empty<ParameterSyntax>())
            .ToList();

        foreach (var param in constructorParameters)
        {
            if (param.Type is null)
                continue;

            var paramType = context.SemanticModel.GetTypeInfo(param.Type).Type as INamedTypeSymbol;
            if (paramType is null)
                continue;

            // Only concrete class types (not interfaces, not structs/enums)
            if (paramType.TypeKind != TypeKind.Class)
                continue;

            // Skip special types (string, object, etc.)
            if (paramType.SpecialType != SpecialType.None)
                continue;

            // Skip if the parameter type is a base class of the current class (inheritance, not wrapping)
            if (IsBaseType(classSymbol, paramType))
                continue;

            // Get public ordinary methods of the wrapped type (excluding common Object methods)
            var wrappedMethods = GetPublicMethodNames(paramType);
            if (wrappedMethods.Count == 0)
                continue;

            // Get public ordinary methods of the current class
            var classMethods = GetPublicMethodNames(classSymbol);

            // A class is a wrapper if it exposes at least one method with the same name as the wrapped type
            if (!wrappedMethods.Overlaps(classMethods))
                continue;

            // This is a wrapper pattern — enforce naming
            var wrappedTypeName = paramType.Name;
            var expectedWrapperSuffix = $"{wrappedTypeName}Wrapper";
            var expectedDecoratorSuffix = $"{wrappedTypeName}Decorator";
            var className = classDeclaration.Identifier.Text;

            // Skip if already correctly named as a Decorator
            if (className.EndsWith(expectedDecoratorSuffix, StringComparison.Ordinal))
                continue;

            if (!className.EndsWith(expectedWrapperSuffix, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    classDeclaration.Identifier.GetLocation(),
                    className,
                    wrappedTypeName));
            }
        }
    }

    private static bool IsBaseType(INamedTypeSymbol type, INamedTypeSymbol potentialBase)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, potentialBase))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static HashSet<string> GetPublicMethodNames(INamedTypeSymbol type)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in type.GetMembers())
        {
            if (member is IMethodSymbol method
                && method.DeclaredAccessibility == Accessibility.Public
                && method.MethodKind == MethodKind.Ordinary
                && !ObjectMethodNames.Contains(method.Name))
            {
                result.Add(method.Name);
            }
        }
        return result;
    }
}
