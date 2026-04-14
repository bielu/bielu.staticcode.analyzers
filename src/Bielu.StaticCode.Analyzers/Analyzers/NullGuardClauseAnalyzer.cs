using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that enforces the use of modern <c>ArgumentNullException.ThrowIfNull()</c>
/// guard clauses instead of manual <c>if (param == null) throw new ArgumentNullException(...)</c>
/// patterns. The modern pattern is more concise and consistently used across the bielu ecosystem.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NullGuardClauseAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU007";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Use ArgumentNullException.ThrowIfNull() for null guard clauses",
        messageFormat: "Use 'ArgumentNullException.ThrowIfNull({0})' instead of manual null check",
        category: "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Null guard clauses should use the modern ArgumentNullException.ThrowIfNull() " +
                     "method instead of manual 'if (param == null) throw new ArgumentNullException(...)' " +
                     "patterns. This is more concise and consistent.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var ifStatement = (IfStatementSyntax)context.Node;

        // Must not have an else clause (simple guard clause)
        if (ifStatement.Else is not null)
            return;

        // Check if condition is a null check pattern
        var parameterName = ExtractNullCheckedParameter(ifStatement.Condition);
        if (parameterName is null)
            return;

        // Check if the body throws ArgumentNullException
        if (!ThrowsArgumentNullException(ifStatement.Statement))
            return;

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add("parameterName", parameterName);

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            ifStatement.GetLocation(),
            properties.ToImmutable(),
            parameterName));
    }

    /// <summary>
    /// Checks if the condition is a null check like:
    /// <c>param == null</c>, <c>param is null</c>, <c>null == param</c>
    /// </summary>
    private static string? ExtractNullCheckedParameter(ExpressionSyntax condition)
    {
        // Pattern: param == null or null == param
        if (condition is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.EqualsExpression))
        {
            if (binary.Right is LiteralExpressionSyntax { Token.Text: "null" } &&
                binary.Left is IdentifierNameSyntax leftId)
                return leftId.Identifier.Text;

            if (binary.Left is LiteralExpressionSyntax { Token.Text: "null" } &&
                binary.Right is IdentifierNameSyntax rightId)
                return rightId.Identifier.Text;
        }

        // Pattern: param is null
        if (condition is IsPatternExpressionSyntax isPattern &&
            isPattern.Pattern is ConstantPatternSyntax { Expression: LiteralExpressionSyntax { Token.Text: "null" } } &&
            isPattern.Expression is IdentifierNameSyntax identifierName)
        {
            return identifierName.Identifier.Text;
        }

        return null;
    }

    /// <summary>
    /// Checks if the statement throws an <c>ArgumentNullException</c>.
    /// Handles both block bodies and single-statement throws.
    /// </summary>
    private static bool ThrowsArgumentNullException(StatementSyntax statement)
    {
        // Single throw statement: if (x == null) throw new ArgumentNullException(...)
        if (statement is ThrowStatementSyntax throwStatement)
            return IsArgumentNullExceptionCreation(throwStatement.Expression);

        // Block with single throw: if (x == null) { throw new ArgumentNullException(...); }
        if (statement is BlockSyntax block &&
            block.Statements.Count == 1 &&
            block.Statements[0] is ThrowStatementSyntax blockThrow)
            return IsArgumentNullExceptionCreation(blockThrow.Expression);

        return false;
    }

    private static bool IsArgumentNullExceptionCreation(ExpressionSyntax? expression)
    {
        if (expression is not ObjectCreationExpressionSyntax creation)
            return false;

        // Check for: new ArgumentNullException(...)
        var typeName = creation.Type switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            _ => null
        };

        return typeName == "ArgumentNullException";
    }
}
