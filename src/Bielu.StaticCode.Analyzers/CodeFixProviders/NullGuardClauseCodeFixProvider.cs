using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bielu.StaticCode.Analyzers.CodeFixProviders;

/// <summary>
/// Code fix that replaces a manual null-check guard clause with
/// <c>ArgumentNullException.ThrowIfNull(parameterName)</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullGuardClauseCodeFixProvider)), Shared]
public sealed class NullGuardClauseCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Analyzers.NullGuardClauseAnalyzer.DiagnosticId);

    public override FixAllProvider? GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document
            .GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null)
            return;

        var diagnostic = context.Diagnostics.First();

        if (!diagnostic.Properties.TryGetValue("parameterName", out var parameterName) ||
            parameterName is null)
            return;

        var ifStatement = root
            .FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent
            ?.AncestorsAndSelf()
            .OfType<IfStatementSyntax>()
            .FirstOrDefault();

        if (ifStatement is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Use ArgumentNullException.ThrowIfNull({parameterName})",
                createChangedDocument: ct =>
                    ReplaceWithThrowIfNullAsync(context.Document, ifStatement, parameterName, ct),
                equivalenceKey: "UseThrowIfNull"),
            diagnostic);
    }

    private static async Task<Document> ReplaceWithThrowIfNullAsync(
        Document document,
        IfStatementSyntax ifStatement,
        string parameterName,
        CancellationToken cancellationToken)
    {
        var root = await document
            .GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        if (root is null)
            return document;

        // Build: ArgumentNullException.ThrowIfNull(parameterName);
        var throwIfNullStatement = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("ArgumentNullException"),
                    SyntaxFactory.IdentifierName("ThrowIfNull")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.IdentifierName(parameterName))))))
            .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
            .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(ifStatement, throwIfNullStatement);
        return document.WithSyntaxRoot(newRoot);
    }
}
