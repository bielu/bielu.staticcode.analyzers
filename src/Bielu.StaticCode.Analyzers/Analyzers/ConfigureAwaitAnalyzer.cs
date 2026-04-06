using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that enforces the use of <c>.ConfigureAwait(false)</c> on await expressions
/// in library code. This prevents deadlocks caused by synchronization context capture
/// and is a best practice for non-UI library code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConfigureAwaitAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU008";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Use ConfigureAwait(false) on awaited tasks",
        messageFormat: "Await expression should use '.ConfigureAwait(false)' to avoid capturing synchronization context",
        category: "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In library code, awaited tasks should always call .ConfigureAwait(false) to " +
                     "avoid capturing the synchronization context. This prevents potential deadlocks " +
                     "and improves performance.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
    }

    private static void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
    {
        var awaitExpression = (AwaitExpressionSyntax)context.Node;

        // Check if the expression already calls ConfigureAwait
        if (HasConfigureAwait(awaitExpression.Expression))
            return;

        // Check that the awaited expression is actually a Task/ValueTask type
        var typeInfo = context.SemanticModel.GetTypeInfo(awaitExpression.Expression);
        if (typeInfo.Type is null)
            return;

        if (!IsConfigurableAwaitableType(typeInfo.Type))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            awaitExpression.GetLocation()));
    }

    private static bool HasConfigureAwait(ExpressionSyntax expression)
    {
        // Check for: await expr.ConfigureAwait(...)
        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "ConfigureAwait")
        {
            return true;
        }

        return false;
    }

    private static bool IsConfigurableAwaitableType(ITypeSymbol type)
    {
        var displayName = type.OriginalDefinition.ToDisplayString();

        return displayName is
            "System.Threading.Tasks.Task" or
            "System.Threading.Tasks.Task<TResult>" or
            "System.Threading.Tasks.ValueTask" or
            "System.Threading.Tasks.ValueTask<TResult>";
    }
}
