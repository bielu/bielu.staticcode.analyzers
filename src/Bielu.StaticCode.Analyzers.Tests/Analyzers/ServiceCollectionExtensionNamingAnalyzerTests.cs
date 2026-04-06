using Bielu.StaticCode.Analyzers.Analyzers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class ServiceCollectionExtensionNamingAnalyzerTests
{
    private readonly ServiceCollectionExtensionNamingAnalyzer _analyzer = new();

    // Force-load the IServiceCollection type
    static ServiceCollectionExtensionNamingAnalyzerTests()
    {
        _ = typeof(IServiceCollection);
    }

    [Fact]
    public async Task CorrectlyNamedExtensionClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.DependencyInjection;

            public static class MyFeatureServiceCollectionExtensions
            {
                public static IServiceCollection AddMyFeature(this IServiceCollection services)
                {
                    return services;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task IncorrectlyNamedExtensionClass_ShouldReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.DependencyInjection;

            public static class MyFeatureExtensions
            {
                public static IServiceCollection AddMyFeature(this IServiceCollection services)
                {
                    return services;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == ServiceCollectionExtensionNamingAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task StaticClassWithoutExtensionMethods_ShouldNotReportDiagnostic()
    {
        const string code = """
            public static class MyHelper
            {
                public static int Add(int a, int b) => a + b;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task NonStaticClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.DependencyInjection;

            public class BadExtensions
            {
                // Not a static class, so extension methods are invalid C# anyway
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExtensionMethodOnOtherType_ShouldNotReportDiagnostic()
    {
        const string code = """
            public static class StringExtensions
            {
                public static string Capitalize(this string input) => input.ToUpper();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }
}
