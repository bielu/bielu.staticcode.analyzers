using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bielu.StaticCode.Analyzers.CodeFixProviders;

/// <summary>
/// Code fix that appends <c>.ConfigureAwait(false)</c> to an awaited expression.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConfigureAwaitCodeFixProvider)), Shared]
public sealed class ConfigureAwaitCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Analyzers.ConfigureAwaitAnalyzer.DiagnosticId);

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
        var awaitExpression = root
            .FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent
            ?.AncestorsAndSelf()
            .OfType<AwaitExpressionSyntax>()
            .FirstOrDefault();

        if (awaitExpression is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add .ConfigureAwait(false)",
                createChangedDocument: ct =>
                    AddConfigureAwaitAsync(context.Document, awaitExpression, ct),
                equivalenceKey: "AddConfigureAwaitFalse"),
            diagnostic);
    }

    private static async Task<Document> AddConfigureAwaitAsync(
        Document document,
        AwaitExpressionSyntax awaitExpression,
        CancellationToken cancellationToken)
    {
        var root = await document
            .GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        if (root is null)
            return document;

        var awaitedExpression = awaitExpression.Expression;

        // Build: awaitedExpression.ConfigureAwait(false)
        var configureAwaitCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                awaitedExpression.WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName("ConfigureAwait")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))))
            .WithTrailingTrivia(awaitedExpression.GetTrailingTrivia());

        var newAwait = awaitExpression.WithExpression(configureAwaitCall);
        var newRoot = root.ReplaceNode(awaitExpression, newAwait);
        return document.WithSyntaxRoot(newRoot);
    }
}
