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
/// constructor parameters with <c>IOptionsMonitor&lt;T&gt;</c> and updates
/// <c>.Value</c> property accesses to <c>.CurrentValue</c>.
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

        var semanticModel = await document
            .GetSemanticModelAsync(cancellationToken)
            .ConfigureAwait(false);

        if (root is null || semanticModel is null || parameter.Type is not GenericNameSyntax genericType)
            return document;

        // Find the containing class to scope the search for .Value accesses
        var containingClass = parameter.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass is null)
            return document;

        // Collect all .Value member accesses on IOptions<T>/IOptionsSnapshot<T> typed expressions
        var valueAccessesToReplace = new Dictionary<SyntaxNode, SyntaxNode>();

        foreach (var memberAccess in containingClass.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (memberAccess.Name.Identifier.Text != "Value")
                continue;

            var expressionTypeInfo = semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken);
            if (expressionTypeInfo.Type is not INamedTypeSymbol namedType)
                continue;

            var typeName = namedType.OriginalDefinition.ToDisplayString();
            if (typeName is not ("Microsoft.Extensions.Options.IOptions<TOptions>" or
                                 "Microsoft.Extensions.Options.IOptionsSnapshot<TOptions>"))
                continue;

            // Also verify the accessed member is actually the Value property
            var symbolInfo = semanticModel.GetSymbolInfo(memberAccess, cancellationToken);
            if (symbolInfo.Symbol is not IPropertySymbol { Name: "Value" })
                continue;

            var newName = SyntaxFactory.IdentifierName("CurrentValue")
                .WithTriviaFrom(memberAccess.Name);
            var newMemberAccess = memberAccess.WithName(newName);
            valueAccessesToReplace[memberAccess] = newMemberAccess;
        }

        // Apply .Value → .CurrentValue replacements first
        if (valueAccessesToReplace.Count > 0)
        {
            root = root.ReplaceNodes(
                valueAccessesToReplace.Keys,
                (original, _) => valueAccessesToReplace[original]);

            // Re-find the parameter's generic type in the updated tree
            var updatedParameter = root.FindToken(parameter.SpanStart).Parent
                ?.AncestorsAndSelf()
                .OfType<ParameterSyntax>()
                .FirstOrDefault();

            if (updatedParameter?.Type is GenericNameSyntax updatedGenericType)
                genericType = updatedGenericType;
            else
                return document.WithSyntaxRoot(root);
        }

        // Replace the parameter type with IOptionsMonitor<T>
        var newType = SyntaxFactory
            .GenericName(
                SyntaxFactory.Identifier("IOptionsMonitor"),
                genericType.TypeArgumentList)
            .WithTriviaFrom(genericType);

        var newRoot = root.ReplaceNode(genericType, newType);
        return document.WithSyntaxRoot(newRoot);
    }
}
