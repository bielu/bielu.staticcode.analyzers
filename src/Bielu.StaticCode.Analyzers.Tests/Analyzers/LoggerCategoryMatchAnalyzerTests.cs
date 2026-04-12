using Bielu.StaticCode.Analyzers.Analyzers;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class LoggerCategoryMatchAnalyzerTests
{
    private readonly LoggerCategoryMatchAnalyzer _analyzer = new();

    // Force-load the ILogger types so they appear in AppDomain.CurrentDomain.GetAssemblies()
    static LoggerCategoryMatchAnalyzerTests()
    {
        _ = typeof(ILogger);
        _ = typeof(ILogger<>);
    }

    [Fact]
    public async Task LoggerWithMatchingType_ShouldNotReportDiagnostic()
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
    public async Task LoggerWithMismatchedType_ShouldReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Logging;

            public class OtherClass { }

            public class MyService
            {
                public MyService(ILogger<OtherClass> logger) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == LoggerCategoryMatchAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task PrimaryConstructorWithMatchingLogger_ShouldNotReportDiagnostic()
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
    public async Task PrimaryConstructorWithMismatchedLogger_ShouldReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Logging;

            public class OtherClass { }

            public class MyService(ILogger<OtherClass> logger)
            {
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == LoggerCategoryMatchAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task UntypedLogger_ShouldNotReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Logging;

            public class MyService
            {
                public MyService(ILogger logger) { }
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
    public async Task CustomGenericILogger_ShouldNotReportDiagnostic()
    {
        const string code = """
            namespace MyApp
            {
                public interface ILogger<T> { void Log(string msg); }

                public class OtherClass { }

                public class MyService
                {
                    public MyService(ILogger<OtherClass> logger) { }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task MultipleConstructorParameters_OnlyLoggerFlagged_ShouldReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Logging;

            public class OtherClass { }

            public class MyService
            {
                public MyService(string name, ILogger<OtherClass> logger, int count) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.Length.ShouldBe(1);
        diagnostics.ShouldContain(d => d.Id == LoggerCategoryMatchAnalyzer.DiagnosticId);
    }
}
