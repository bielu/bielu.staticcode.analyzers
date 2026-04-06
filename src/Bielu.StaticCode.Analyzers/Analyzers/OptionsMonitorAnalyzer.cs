using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that enforces the use of <c>IOptionsMonitor&lt;T&gt;</c> over
/// <c>IOptions&lt;T&gt;</c> and <c>IOptionsSnapshot&lt;T&gt;</c>.
/// <c>IOptionsMonitor&lt;T&gt;</c> provides the current value and supports
/// change notifications, making it preferable for hot-reload scenarios.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptionsMonitorAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU003";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Use IOptionsMonitor<T> instead of IOptions<T> or IOptionsSnapshot<T>",
        messageFormat: "Parameter '{0}' uses '{1}<{2}>' but should use 'IOptionsMonitor<{2}>' to support hot-reload",
        category: "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Options should be injected as IOptionsMonitor<T> rather than IOptions<T> or " +
                     "IOptionsSnapshot<T>. IOptionsMonitor<T> supports change notifications and " +
                     "hot-reload of configuration values.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeParameters(ctx, ((ConstructorDeclarationSyntax)ctx.Node).ParameterList.Parameters),
            SyntaxKind.ConstructorDeclaration);
        context.RegisterSyntaxNodeAction(
            ctx =>
            {
                var classDecl = (ClassDeclarationSyntax)ctx.Node;
                if (classDecl.ParameterList is not null)
                    AnalyzeParameters(ctx, classDecl.ParameterList.Parameters);
            },
            SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeParameters(
        SyntaxNodeAnalysisContext context,
        SeparatedSyntaxList<ParameterSyntax> parameters)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.Type is not GenericNameSyntax genericType)
                continue;

            var typeName = genericType.Identifier.Text;

            if (typeName is not ("IOptions" or "IOptionsSnapshot"))
                continue;

            // Verify via semantic model that the type is from Microsoft.Extensions.Options
            var typeInfo = context.SemanticModel.GetTypeInfo(parameter.Type);
            if (typeInfo.Type is not INamedTypeSymbol namedType)
                continue;

            var ns = namedType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (!ns.StartsWith("Microsoft.Extensions.Options", StringComparison.Ordinal))
                continue;

            var typeArgument = genericType.TypeArgumentList.Arguments.Count == 1
                ? genericType.TypeArgumentList.Arguments[0].ToString()
                : "T";

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                parameter.GetLocation(),
                parameter.Identifier.Text,
                typeName,
                typeArgument));
        }
    }
}
