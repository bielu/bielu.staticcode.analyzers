using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bielu.StaticCode.Analyzers.CodeFixProviders;

/// <summary>
/// Code fix that adds the <c>sealed</c> modifier to an internal class.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SealedInternalClassCodeFixProvider)), Shared]
public sealed class SealedInternalClassCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Analyzers.SealedInternalClassAnalyzer.DiagnosticId);

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
        var classDeclaration = root
            .FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent
            ?.AncestorsAndSelf()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (classDeclaration is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add 'sealed' modifier",
                createChangedDocument: ct =>
                    AddSealedModifierAsync(context.Document, classDeclaration, ct),
                equivalenceKey: "AddSealedModifier"),
            diagnostic);
    }

    private static async Task<Document> AddSealedModifierAsync(
        Document document,
        ClassDeclarationSyntax classDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document
            .GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        if (root is null)
            return document;

        // Insert 'sealed' after any access modifier, or at the beginning
        var sealedToken = SyntaxFactory.Token(SyntaxKind.SealedKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        var modifiers = classDeclaration.Modifiers;
        int insertIndex = 0;

        // Find the position after access modifiers (internal, etc.)
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].IsKind(SyntaxKind.InternalKeyword) ||
                modifiers[i].IsKind(SyntaxKind.PublicKeyword) ||
                modifiers[i].IsKind(SyntaxKind.ProtectedKeyword) ||
                modifiers[i].IsKind(SyntaxKind.PrivateKeyword))
            {
                insertIndex = i + 1;
            }
        }

        var newModifiers = modifiers.Insert(insertIndex, sealedToken);
        var newClassDeclaration = classDeclaration.WithModifiers(newModifiers);
        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}
