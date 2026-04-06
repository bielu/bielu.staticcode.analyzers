using Bielu.StaticCode.Analyzers.Analyzers;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class ConfigureAwaitAnalyzerTests
{
    private readonly ConfigureAwaitAnalyzer _analyzer = new();

    [Fact]
    public async Task AwaitWithConfigureAwaitFalse_ShouldNotReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public async Task DoWorkAsync()
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task AwaitWithoutConfigureAwait_ShouldReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public async Task DoWorkAsync()
                {
                    await Task.Delay(100);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == ConfigureAwaitAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task AwaitWithConfigureAwaitTrue_ShouldNotReportDiagnostic()
    {
        // ConfigureAwait(true) is still ConfigureAwait — the developer made an explicit choice
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public async Task DoWorkAsync()
                {
                    await Task.Delay(100).ConfigureAwait(true);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task MultipleAwaitsWithMixedConfigureAwait_ShouldReportOnlyMissing()
    {
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public async Task DoWorkAsync()
                {
                    await Task.Delay(100).ConfigureAwait(false);
                    await Task.Delay(200);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.Length.ShouldBe(1);
        diagnostics[0].Id.ShouldBe(ConfigureAwaitAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task AwaitTaskFromResult_WithoutConfigureAwait_ShouldReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public async Task<int> GetValueAsync()
                {
                    return await Task.FromResult(42);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == ConfigureAwaitAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task AwaitValueTask_WithoutConfigureAwait_ShouldReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public async ValueTask DoWorkAsync()
                {
                    await Task.Delay(100);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == ConfigureAwaitAnalyzer.DiagnosticId);
    }
}
