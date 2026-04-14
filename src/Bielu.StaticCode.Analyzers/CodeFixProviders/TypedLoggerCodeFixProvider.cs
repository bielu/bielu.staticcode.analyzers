using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bielu.StaticCode.Analyzers.CodeFixProviders;

/// <summary>
/// Code fix that replaces untyped <c>ILogger</c> with <c>ILogger&lt;ClassName&gt;</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TypedLoggerCodeFixProvider)), Shared]
public sealed class TypedLoggerCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Analyzers.TypedLoggerAnalyzer.DiagnosticId);

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

        if (!diagnostic.Properties.TryGetValue("className", out var className) ||
            className is null)
            return;

        var parameter = root
            .FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent
            ?.AncestorsAndSelf()
            .OfType<ParameterSyntax>()
            .FirstOrDefault();

        if (parameter?.Type is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Use ILogger<{className}>",
                createChangedDocument: ct =>
                    ReplaceWithTypedLoggerAsync(context.Document, parameter, className, ct),
                equivalenceKey: "UseTypedLogger"),
            diagnostic);
    }

    private static async Task<Document> ReplaceWithTypedLoggerAsync(
        Document document,
        ParameterSyntax parameter,
        string className,
        CancellationToken cancellationToken)
    {
        var root = await document
            .GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        if (root is null || parameter.Type is null)
            return document;

        // Build: ILogger<ClassName>
        var newType = SyntaxFactory.GenericName(
            SyntaxFactory.Identifier("ILogger"),
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                    SyntaxFactory.IdentifierName(className))))
            .WithTriviaFrom(parameter.Type);

        var newRoot = root.ReplaceNode(parameter.Type, newType);
        return document.WithSyntaxRoot(newRoot);
    }
}
