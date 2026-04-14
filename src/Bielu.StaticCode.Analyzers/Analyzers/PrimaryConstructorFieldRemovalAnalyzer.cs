using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that recommends removal of private fields when a class uses a primary constructor
/// and the field is simply assigned from a primary constructor parameter.
/// The primary constructor parameter can be used directly instead.
/// Fields whose corresponding parameter is passed to a base class constructor are excluded,
/// as the field is still needed in that scenario.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrimaryConstructorFieldRemovalAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU012";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Remove private field in favor of primary constructor parameter",
        messageFormat: "Private field '{0}' can be removed; use primary constructor parameter '{1}' directly",
        category: "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a class uses a primary constructor, private fields that are simply assigned " +
                     "from a primary constructor parameter are redundant. Use the parameter directly instead.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Only applies to classes that use a primary constructor
        if (classDeclaration.ParameterList is null)
            return;

        var primaryCtorParameters = classDeclaration.ParameterList.Parameters;
        if (primaryCtorParameters.Count == 0)
            return;

        // Build set of parameter names that are passed to a base class constructor.
        // Fields backed by these parameters should NOT be flagged.
        var basePassedParameters = GetBaseConstructorArguments(classDeclaration);

        // Examine each field declaration in the class
        foreach (var member in classDeclaration.Members)
        {
            if (member is not FieldDeclarationSyntax fieldDeclaration)
                continue;

            // Only target private fields (with or without readonly)
            if (!IsPrivateField(fieldDeclaration))
                continue;

            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                // The field must have an initializer
                if (variable.Initializer?.Value is not IdentifierNameSyntax identifierName)
                    continue;

                var initializerName = identifierName.Identifier.Text;

                // Check if the initializer references a primary constructor parameter
                var matchingParameter = primaryCtorParameters
                    .FirstOrDefault(p => p.Identifier.Text == initializerName);

                if (matchingParameter == null)
                    continue;

                // Skip if this parameter is passed to a base class constructor
                if (basePassedParameters.Contains(initializerName))
                    continue;

                // Verify via semantic model that the identifier actually resolves to
                // the primary constructor parameter (not some other symbol in scope)
                var symbolInfo = context.SemanticModel.GetSymbolInfo(identifierName, context.CancellationToken);
                if (symbolInfo.Symbol is not IParameterSymbol parameterSymbol)
                    continue;

                // Confirm the parameter belongs to the containing type (primary constructor parameter)
                if (parameterSymbol.ContainingSymbol is not IMethodSymbol methodSymbol ||
                    methodSymbol.MethodKind != MethodKind.Constructor)
                    continue;

                var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);
                if (classSymbol == null ||
                    !SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, classSymbol))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    variable.GetLocation(),
                    variable.Identifier.Text,
                    initializerName));
            }
        }
    }

    /// <summary>
    /// Collects the names of identifiers passed as arguments to the base class constructor
    /// in the class's base list (e.g., <c>: Base(paramName)</c>).
    /// </summary>
    private static ImmutableHashSet<string> GetBaseConstructorArguments(ClassDeclarationSyntax classDeclaration)
    {
        var builder = ImmutableHashSet.CreateBuilder<string>();

        if (classDeclaration.BaseList is null)
            return builder.ToImmutable();

        foreach (var baseType in classDeclaration.BaseList.Types)
        {
            if (baseType is PrimaryConstructorBaseTypeSyntax primaryBase)
            {
                foreach (var arg in primaryBase.ArgumentList.Arguments)
                {
                    if (arg.Expression is IdentifierNameSyntax identifier)
                    {
                        builder.Add(identifier.Identifier.Text);
                    }
                }
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Returns true when the field declaration is private (explicit or default)
    /// with no other accessibility modifiers.
    /// </summary>
    private static bool IsPrivateField(FieldDeclarationSyntax fieldDeclaration)
    {
        var modifiers = fieldDeclaration.Modifiers;

        // Explicit private modifier
        if (modifiers.Any(SyntaxKind.PrivateKeyword))
            return true;

        // No accessibility modifier at all means private by default in a class,
        // but only if there is no public/protected/internal modifier
        bool hasAccessibility = modifiers.Any(SyntaxKind.PublicKeyword) ||
                                modifiers.Any(SyntaxKind.ProtectedKeyword) ||
                                modifiers.Any(SyntaxKind.InternalKeyword);

        return !hasAccessibility;
    }
}
