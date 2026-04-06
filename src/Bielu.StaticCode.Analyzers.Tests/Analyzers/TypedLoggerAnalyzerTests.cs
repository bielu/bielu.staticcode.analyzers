using Bielu.StaticCode.Analyzers.Analyzers;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class TypedLoggerAnalyzerTests
{
    private readonly TypedLoggerAnalyzer _analyzer = new();

    // Force-load the ILogger types so they appear in AppDomain.CurrentDomain.GetAssemblies()
    static TypedLoggerAnalyzerTests()
    {
        _ = typeof(ILogger);
        _ = typeof(ILogger<>);
    }

    [Fact]
    public async Task TypedLoggerParameter_ShouldNotReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Logging;

            public class MyService
            {
                public MyService(ILogger<MyService> logger) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task UntypedLoggerParameter_ShouldReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Logging;

            public class MyService
            {
                public MyService(ILogger logger) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == TypedLoggerAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task PrimaryConstructorWithUntypedLogger_ShouldReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Logging;

            public class MyService(ILogger logger)
            {
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == TypedLoggerAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task PrimaryConstructorWithTypedLogger_ShouldNotReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Logging;

            public class MyService(ILogger<MyService> logger)
            {
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task NonLoggerParameter_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService
            {
                public MyService(string name) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task CustomILoggerInterface_ShouldNotReportDiagnostic()
    {
        const string code = """
            namespace MyApp
            {
                public interface ILogger { void Log(string msg); }

                public class MyService
                {
                    public MyService(ILogger logger) { }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }
}
