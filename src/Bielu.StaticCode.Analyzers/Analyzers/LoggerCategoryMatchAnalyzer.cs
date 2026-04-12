using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that enforces the type parameter of <c>ILogger&lt;T&gt;</c> must match the containing class.
/// For example, in class <c>MyService</c>, the logger should be <c>ILogger&lt;MyService&gt;</c>,
/// not <c>ILogger&lt;OtherClass&gt;</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LoggerCategoryMatchAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU011";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "ILogger<T> category type must match containing class",
        messageFormat: "Parameter '{0}' uses 'ILogger<{1}>' but should use 'ILogger<{2}>' to match the containing class",
        category: "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When using ILogger<T> in a class, the type parameter T should match the containing class. " +
                     "For example, in class MyService, use ILogger<MyService> instead of ILogger<OtherClass>.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeConstructor(ctx, (ConstructorDeclarationSyntax)ctx.Node),
            SyntaxKind.ConstructorDeclaration);
        context.RegisterSyntaxNodeAction(
            ctx => AnalyzePrimaryConstructor(ctx, (ClassDeclarationSyntax)ctx.Node),
            SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context, ConstructorDeclarationSyntax constructor)
    {
        var containingClass = constructor.Parent as ClassDeclarationSyntax;
        if (containingClass is null)
            return;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(containingClass);
        if (classSymbol is null)
            return;

        AnalyzeParameters(context, constructor.ParameterList.Parameters, classSymbol);
    }

    private static void AnalyzePrimaryConstructor(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDecl)
    {
        if (classDecl.ParameterList is null)
            return;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        if (classSymbol is null)
            return;

        AnalyzeParameters(context, classDecl.ParameterList.Parameters, classSymbol);
    }

    private static void AnalyzeParameters(
        SyntaxNodeAnalysisContext context,
        SeparatedSyntaxList<ParameterSyntax> parameters,
        INamedTypeSymbol containingClass)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.Type is null)
                continue;

            // We are looking for generic ILogger<T> (GenericNameSyntax)
            if (parameter.Type is not GenericNameSyntax genericName)
                continue;

            if (genericName.Identifier.Text != "ILogger")
                continue;

            if (genericName.TypeArgumentList.Arguments.Count != 1)
                continue;

            // Verify it's from Microsoft.Extensions.Logging
            var typeInfo = context.SemanticModel.GetTypeInfo(parameter.Type);
            if (typeInfo.Type is not INamedTypeSymbol namedType)
                continue;

            var ns = namedType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (!ns.StartsWith("Microsoft.Extensions.Logging", StringComparison.Ordinal))
                continue;

            if (!namedType.IsGenericType || namedType.TypeArguments.Length != 1)
                continue;

            var loggerTypeArg = namedType.TypeArguments[0];

            // Check if the type argument matches the containing class
            if (!SymbolEqualityComparer.Default.Equals(loggerTypeArg, containingClass))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    parameter.GetLocation(),
                    parameter.Identifier.Text,
                    loggerTypeArg.Name,
                    containingClass.Name));
            }
        }
    }
}
