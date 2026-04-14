using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bielu.StaticCode.Analyzers.CodeFixProviders;

/// <summary>
/// Code fix that replaces the type argument in <c>ILogger&lt;Wrong&gt;</c> with the containing class name.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LoggerCategoryMatchCodeFixProvider)), Shared]
public sealed class LoggerCategoryMatchCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Analyzers.LoggerCategoryMatchAnalyzer.DiagnosticId);

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

        if (!diagnostic.Properties.TryGetValue("expectedClassName", out var expectedClassName) ||
            expectedClassName is null)
            return;

        var parameter = root
            .FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent
            ?.AncestorsAndSelf()
            .OfType<ParameterSyntax>()
            .FirstOrDefault();

        if (parameter?.Type is not GenericNameSyntax)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Change to ILogger<{expectedClassName}>",
                createChangedDocument: ct =>
                    ReplaceTypeArgumentAsync(context.Document, parameter, expectedClassName, ct),
                equivalenceKey: "FixLoggerCategory"),
            diagnostic);
    }

    private static async Task<Document> ReplaceTypeArgumentAsync(
        Document document,
        ParameterSyntax parameter,
        string expectedClassName,
        CancellationToken cancellationToken)
    {
        var root = await document
            .GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        if (root is null || parameter.Type is not GenericNameSyntax genericType)
            return document;

        // Build: ILogger<ExpectedClassName>
        var newType = SyntaxFactory.GenericName(
            SyntaxFactory.Identifier("ILogger"),
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                    SyntaxFactory.IdentifierName(expectedClassName))))
            .WithTriviaFrom(genericType);

        var newRoot = root.ReplaceNode(genericType, newType);
        return document.WithSyntaxRoot(newRoot);
    }
}
