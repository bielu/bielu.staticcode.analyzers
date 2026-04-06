using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that enforces the naming convention for <c>IServiceCollection</c> extension classes.
/// Classes containing extension methods on <c>IServiceCollection</c> should be named
/// <c>{Feature}ServiceCollectionExtensions</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ServiceCollectionExtensionNamingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU006";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "IServiceCollection extension class naming convention violation",
        messageFormat: "Class '{0}' contains IServiceCollection extension methods and should be named '{{Feature}}ServiceCollectionExtensions'",
        category: "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Static classes containing extension methods on IServiceCollection should follow " +
                     "the naming pattern: {Feature}ServiceCollectionExtensions.");

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

        // Only applies to static classes
        if (!classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            return;

        // Check if the class name already follows the convention
        var className = classDeclaration.Identifier.Text;
        if (className.EndsWith("ServiceCollectionExtensions", StringComparison.Ordinal))
            return;

        // Check if the class has any extension methods on IServiceCollection
        var hasServiceCollectionExtensions = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Any(method => IsServiceCollectionExtensionMethod(context, method));

        if (!hasServiceCollectionExtensions)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            classDeclaration.Identifier.GetLocation(),
            className));
    }

    private static bool IsServiceCollectionExtensionMethod(
        SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax method)
    {
        // Must be a static method
        if (!method.Modifiers.Any(SyntaxKind.StaticKeyword))
            return false;

        // Must have at least one parameter with `this` modifier (extension method)
        var firstParam = method.ParameterList.Parameters.FirstOrDefault();
        if (firstParam is null)
            return false;

        if (!firstParam.Modifiers.Any(SyntaxKind.ThisKeyword))
            return false;

        // Check if the first parameter's type is IServiceCollection
        if (firstParam.Type is null)
            return false;

        var typeInfo = context.SemanticModel.GetTypeInfo(firstParam.Type);
        if (typeInfo.Type is null)
            return false;

        var typeName = typeInfo.Type.ToDisplayString();
        return typeName == "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    }
}
