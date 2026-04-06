using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that encourages use of primary constructors.
/// A class with a single constructor whose body only contains simple field/property
/// assignments should be rewritten using a primary constructor (C# 12+).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrimaryConstructorAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Use primary constructor",
        messageFormat: "Class '{0}' should use a primary constructor",
        category: "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Classes with a single constructor whose body only performs field or property " +
                     "assignments should use the primary constructor syntax introduced in C# 12.");

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

        // Skip if the class already uses a primary constructor
        if (classDeclaration.ParameterList is not null)
            return;

        // Skip abstract and static classes
        if (classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
            classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            return;

        var constructors = classDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .ToList();

        // Primary constructor is only applicable when there is exactly one constructor
        if (constructors.Count != 1)
            return;

        var constructor = constructors[0];

        // The constructor must have at least one parameter to benefit from primary constructor
        if (!constructor.ParameterList.Parameters.Any())
            return;

        // Skip constructors that chain to another constructor (this(...) or base(...))
        if (constructor.Initializer is not null)
            return;

        // The constructor body must consist solely of simple field/property assignments
        if (constructor.Body is not null && !constructor.Body.Statements.All(IsSimpleAssignment))
            return;

        // Expression-body constructors are unusual; skip to be safe
        if (constructor.ExpressionBody is not null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            constructor.GetLocation(),
            classDeclaration.Identifier.Text));
    }

    /// <summary>
    /// Returns true when the statement is a simple assignment of the form
    /// <c>_field = param;</c> or <c>this.Property = param;</c>.
    /// </summary>
    private static bool IsSimpleAssignment(StatementSyntax statement)
    {
        if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment })
            return false;

        if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            return false;

        // Right-hand side must be a plain identifier (the constructor parameter)
        if (assignment.Right is not IdentifierNameSyntax)
            return false;

        // Left-hand side: _field or this.Property
        return assignment.Left switch
        {
            IdentifierNameSyntax => true,
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } => true,
            _ => false,
        };
    }
}
