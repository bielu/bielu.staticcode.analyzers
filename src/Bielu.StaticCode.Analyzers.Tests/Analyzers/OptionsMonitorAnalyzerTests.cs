using Bielu.StaticCode.Analyzers.Analyzers;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class OptionsMonitorAnalyzerTests
{
    private readonly OptionsMonitorAnalyzer _analyzer = new();

    // Force the Microsoft.Extensions.Options assembly to be loaded before tests run,
    // so it appears in AppDomain.CurrentDomain.GetAssemblies() and the test helper
    // can include it as a compilation reference.
    static OptionsMonitorAnalyzerTests()
    {
        _ = typeof(IOptions<>);
        _ = typeof(IOptionsMonitor<>);
        _ = typeof(IOptionsSnapshot<>);
    }

    [Fact]
    public async Task IOptionsMonitorParameter_ShouldNotReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Options;

            public class MyOptions { }

            public class MyService
            {
                public MyService(IOptionsMonitor<MyOptions> options) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task IOptionsParameter_ShouldReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Options;

            public class MyOptions { }

            public class MyService
            {
                public MyService(IOptions<MyOptions> options) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == OptionsMonitorAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task IOptionsSnapshotParameter_ShouldReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Options;

            public class MyOptions { }

            public class MyService
            {
                public MyService(IOptionsSnapshot<MyOptions> options) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == OptionsMonitorAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task PrimaryConstructorWithIOptions_ShouldReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Options;

            public class MyOptions { }

            public class MyService(IOptions<MyOptions> options)
            {
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == OptionsMonitorAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task PrimaryConstructorWithIOptionsMonitor_ShouldNotReportDiagnostic()
    {
        const string code = """
            using Microsoft.Extensions.Options;

            public class MyOptions { }

            public class MyService(IOptionsMonitor<MyOptions> options)
            {
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task NonOptionsGenericParameter_ShouldNotReportDiagnostic()
    {
        const string code = """
            using System.Collections.Generic;

            public class MyService
            {
                public MyService(IList<string> items) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task MultipleParameters_OnlyOptionsOnesReportDiagnostics()
    {
        const string code = """
            using Microsoft.Extensions.Options;
            using Microsoft.Extensions.Logging;

            public class MyOptions { }

            public class MyService
            {
                public MyService(IOptions<MyOptions> options, ILogger<MyService> logger) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.Length.ShouldBe(1);
        diagnostics[0].Id.ShouldBe(OptionsMonitorAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task IOptionsWithCustomNamespace_ShouldNotReportDiagnostic()
    {
        // A custom IOptions<T> from a different namespace should not trigger the rule
        const string code = """
            namespace MyApp
            {
                public class MyOptions { }

                public interface IOptions<T> { T Value { get; } }

                public class MyService
                {
                    public MyService(IOptions<MyOptions> options) { }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }
}
