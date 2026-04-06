using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bielu.StaticCode.Analyzers.CodeFixProviders;

/// <summary>
/// Code fix that replaces <c>IOptions&lt;T&gt;</c> or <c>IOptionsSnapshot&lt;T&gt;</c>
/// constructor parameters with <c>IOptionsMonitor&lt;T&gt;</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(OptionsMonitorCodeFixProvider)), Shared]
public sealed class OptionsMonitorCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Analyzers.OptionsMonitorAnalyzer.DiagnosticId);

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
        var parameter = root
            .FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent
            ?.AncestorsAndSelf()
            .OfType<ParameterSyntax>()
            .FirstOrDefault();

        if (parameter is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use IOptionsMonitor<T>",
                createChangedDocument: ct =>
                    ReplaceWithOptionsMonitorAsync(context.Document, parameter, ct),
                equivalenceKey: "UseIOptionsMonitor"),
            diagnostic);
    }

    private static async Task<Document> ReplaceWithOptionsMonitorAsync(
        Document document,
        ParameterSyntax parameter,
        CancellationToken cancellationToken)
    {
        var root = await document
            .GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        if (root is null || parameter.Type is not GenericNameSyntax genericType)
            return document;

        var newType = SyntaxFactory
            .GenericName(
                SyntaxFactory.Identifier("IOptionsMonitor"),
                genericType.TypeArgumentList)
            .WithTriviaFrom(genericType);

        var newRoot = root.ReplaceNode(genericType, newType);
        return document.WithSyntaxRoot(newRoot);
    }
}
