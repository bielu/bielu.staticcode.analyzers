using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that enforces the use of typed <c>ILogger&lt;T&gt;</c> over untyped <c>ILogger</c>
/// for constructor/primary constructor injection. Typed loggers provide better filtering
/// and categorization in logging frameworks.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypedLoggerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU004";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Use ILogger<T> instead of ILogger",
        messageFormat: "Parameter '{0}' uses untyped 'ILogger' but should use 'ILogger<{1}>' for better log categorization",
        category: "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Constructor parameters should use ILogger<T> (where T is the containing class) " +
                     "instead of the untyped ILogger interface. Typed loggers enable proper log " +
                     "categorization and filtering.");

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

        var className = containingClass.Identifier.Text;
        AnalyzeParameters(context, constructor.ParameterList.Parameters, className);
    }

    private static void AnalyzePrimaryConstructor(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDecl)
    {
        if (classDecl.ParameterList is null)
            return;

        AnalyzeParameters(context, classDecl.ParameterList.Parameters, classDecl.Identifier.Text);
    }

    private static void AnalyzeParameters(
        SyntaxNodeAnalysisContext context,
        SeparatedSyntaxList<ParameterSyntax> parameters,
        string className)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.Type is null)
                continue;

            // Check for non-generic ILogger (not ILogger<T>)
            if (parameter.Type is not IdentifierNameSyntax identifierName)
                continue;

            if (identifierName.Identifier.Text != "ILogger")
                continue;

            // Verify it's from Microsoft.Extensions.Logging
            var typeInfo = context.SemanticModel.GetTypeInfo(parameter.Type);
            if (typeInfo.Type is not INamedTypeSymbol namedType)
                continue;

            var ns = namedType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (!ns.StartsWith("Microsoft.Extensions.Logging", StringComparison.Ordinal))
                continue;

            // Only flag non-generic ILogger (ILogger<T> is already fine)
            if (namedType.IsGenericType)
                continue;

            var properties = ImmutableDictionary.CreateBuilder<string, string?>();
            properties.Add("className", className);

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                parameter.GetLocation(),
                properties.ToImmutable(),
                parameter.Identifier.Text,
                className));
        }
    }
}
