using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bielu.StaticCode.Analyzers.CodeFixProviders;

/// <summary>
/// Code fix that removes a redundant private field and replaces all its usages
/// with the corresponding primary constructor parameter.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrimaryConstructorFieldRemovalCodeFixProvider)), Shared]
public sealed class PrimaryConstructorFieldRemovalCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Analyzers.PrimaryConstructorFieldRemovalAnalyzer.DiagnosticId);

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

        if (!diagnostic.Properties.TryGetValue("fieldName", out var fieldName) ||
            !diagnostic.Properties.TryGetValue("parameterName", out var parameterName) ||
            fieldName is null || parameterName is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Remove field '{fieldName}' and use parameter '{parameterName}'",
                createChangedDocument: ct =>
                    RemoveFieldAndReplaceUsagesAsync(context.Document, diagnostic, fieldName, parameterName, ct),
                equivalenceKey: "RemoveFieldUseParameter"),
            diagnostic);
    }

    private static async Task<Document> RemoveFieldAndReplaceUsagesAsync(
        Document document,
        Diagnostic diagnostic,
        string fieldName,
        string parameterName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return document;

        // Find the variable declarator from the diagnostic location
        var diagnosticNode = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent;
        var variableDeclarator = diagnosticNode?.AncestorsAndSelf()
            .OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault();

        if (variableDeclarator is null)
            return document;

        // Get the field symbol
        var fieldSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken) as IFieldSymbol;
        if (fieldSymbol is null)
            return document;

        // Find the enclosing class declaration
        var classDeclaration = variableDeclarator.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclaration is null)
            return document;

        // Collect all nodes that reference this field within the class
        var nodesToReplace = new Dictionary<SyntaxNode, SyntaxNode>();

        foreach (var node in classDeclaration.DescendantNodes())
        {
            switch (node)
            {
                // Replace `this._field` with `parameterName`
                case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } memberAccess
                    when memberAccess.Name is IdentifierNameSyntax memberName:
                {
                    var symbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
                    if (symbol is not null && SymbolEqualityComparer.Default.Equals(symbol, fieldSymbol))
                    {
                        nodesToReplace[memberAccess] = SyntaxFactory.IdentifierName(parameterName)
                            .WithTriviaFrom(memberAccess);
                    }
                    break;
                }

                // Replace `_field` with `parameterName`
                case IdentifierNameSyntax identifierName
                    when identifierName.Identifier.Text == fieldName:
                {
                    // Skip if this is part of the field declaration itself (the variable declarator)
                    if (identifierName.Ancestors().OfType<VariableDeclaratorSyntax>().Any(v => v == variableDeclarator))
                        continue;

                    // Skip if already handled as part of a this.member access
                    if (identifierName.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax })
                        continue;

                    var symbol = semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol;
                    if (symbol is not null && SymbolEqualityComparer.Default.Equals(symbol, fieldSymbol))
                    {
                        nodesToReplace[identifierName] = SyntaxFactory.IdentifierName(parameterName)
                            .WithTriviaFrom(identifierName);
                    }
                    break;
                }
            }
        }

        // Apply all replacements first
        var newRoot = root.ReplaceNodes(
            nodesToReplace.Keys,
            (original, _) => nodesToReplace[original]);

        // After replacements, remove the field declaration.
        // Re-find the variable declarator in the updated tree.
        var updatedDeclarator = newRoot.FindToken(diagnostic.Location.SourceSpan.Start).Parent
            ?.AncestorsAndSelf()
            .OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault();

        if (updatedDeclarator is null)
            return document.WithSyntaxRoot(newRoot);

        var fieldDeclaration = updatedDeclarator.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
        if (fieldDeclaration is null)
            return document.WithSyntaxRoot(newRoot);

        // If the field declaration has multiple variables, only remove the specific declarator
        if (fieldDeclaration.Declaration.Variables.Count > 1)
        {
            var newDeclaration = fieldDeclaration.Declaration.RemoveNode(updatedDeclarator, SyntaxRemoveOptions.KeepNoTrivia);
            if (newDeclaration is not null)
            {
                newRoot = newRoot.ReplaceNode(fieldDeclaration.Declaration, newDeclaration);
            }
        }
        else
        {
            // Remove the entire field declaration
            newRoot = newRoot.RemoveNode(fieldDeclaration, SyntaxRemoveOptions.KeepNoTrivia)!;
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
