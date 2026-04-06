using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bielu.StaticCode.Analyzers.Analyzers;

/// <summary>
/// Analyzer that enforces the <c>Async</c> suffix on methods that return
/// <c>Task</c>, <c>Task&lt;T&gt;</c>, <c>ValueTask</c>, or <c>ValueTask&lt;T&gt;</c>.
/// This convention makes the asynchronous nature of methods immediately visible at call sites.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncMethodNamingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BIELU005";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Async method should have 'Async' suffix",
        messageFormat: "Method '{0}' returns '{1}' but its name does not end with 'Async'",
        category: "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Methods that return Task, Task<T>, ValueTask, or ValueTask<T> should have " +
                     "their name suffixed with 'Async' to clearly indicate their asynchronous nature.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeLocalFunctionStatement, SyntaxKind.LocalFunctionStatement);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;

        // Skip entry points (Main), interface implementations are handled by convention
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(method);
        if (methodSymbol is null)
            return;

        // Skip overridden methods and explicit interface implementations —
        // the naming is dictated by the base/interface
        if (methodSymbol.IsOverride || methodSymbol.ExplicitInterfaceImplementations.Length > 0)
            return;

        // Skip methods that implement an interface method (implicit implementation)
        if (IsInterfaceImplementation(methodSymbol))
            return;

        // Skip test methods (methods with xUnit/NUnit/MSTest attributes)
        if (IsTestMethod(methodSymbol))
            return;

        CheckAsyncSuffix(context, method.Identifier, method.ReturnType);
    }

    private static void AnalyzeLocalFunctionStatement(SyntaxNodeAnalysisContext context)
    {
        var localFunction = (LocalFunctionStatementSyntax)context.Node;
        CheckAsyncSuffix(context, localFunction.Identifier, localFunction.ReturnType);
    }

    private static void CheckAsyncSuffix(
        SyntaxNodeAnalysisContext context,
        SyntaxToken identifier,
        TypeSyntax returnType)
    {
        var methodName = identifier.Text;

        // Already ends with Async — nothing to report
        if (methodName.EndsWith("Async", StringComparison.Ordinal))
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(returnType);
        if (typeInfo.Type is null)
            return;

        if (!IsAsyncReturnType(typeInfo.Type))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            identifier.GetLocation(),
            methodName,
            typeInfo.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static bool IsAsyncReturnType(ITypeSymbol type)
    {
        var displayName = type.OriginalDefinition.ToDisplayString();

        return displayName is
            "System.Threading.Tasks.Task" or
            "System.Threading.Tasks.Task<TResult>" or
            "System.Threading.Tasks.ValueTask" or
            "System.Threading.Tasks.ValueTask<TResult>";
    }

    private static bool IsInterfaceImplementation(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
            return false;

        foreach (var iface in containingType.AllInterfaces)
        {
            foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
            {
                var impl = containingType.FindImplementationForInterfaceMember(member);
                if (SymbolEqualityComparer.Default.Equals(impl, method))
                    return true;
            }
        }

        return false;
    }

    private static bool IsTestMethod(IMethodSymbol method)
    {
        return method.GetAttributes().Any(attr =>
        {
            var name = attr.AttributeClass?.Name ?? string.Empty;
            return name is "FactAttribute" or "TheoryAttribute" or   // xUnit
                          "TestAttribute" or "TestCaseAttribute" or  // NUnit
                          "TestMethodAttribute";                     // MSTest
        });
    }
}
