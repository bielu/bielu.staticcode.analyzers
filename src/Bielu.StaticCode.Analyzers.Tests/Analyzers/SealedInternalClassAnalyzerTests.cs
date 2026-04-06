using Bielu.StaticCode.Analyzers.Analyzers;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class SealedInternalClassAnalyzerTests
{
    private readonly SealedInternalClassAnalyzer _analyzer = new();

    [Fact]
    public async Task InternalClassWithoutSealed_ShouldReportDiagnostic()
    {
        const string code = """
            internal class MyHelper
            {
                public void DoWork() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == SealedInternalClassAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task SealedInternalClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            internal sealed class MyHelper
            {
                public void DoWork() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task PublicClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyHelper
            {
                public void DoWork() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task AbstractInternalClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            internal abstract class BaseHelper
            {
                public abstract void DoWork();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task StaticInternalClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            internal static class Helper
            {
                public static void DoWork() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task InternalClassWithVirtualMethod_ShouldNotReportDiagnostic()
    {
        const string code = """
            internal class BaseService
            {
                public virtual void DoWork() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task InternalClassWithDerivedClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            internal class BaseService
            {
                public void DoWork() { }
            }

            internal sealed class DerivedService : BaseService
            {
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        // BaseService has a derived class so it should not be flagged
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task DefaultAccessibilityClass_ShouldReportDiagnostic()
    {
        // Default accessibility for top-level classes is internal
        const string code = """
            class MyHelper
            {
                public void DoWork() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == SealedInternalClassAnalyzer.DiagnosticId);
    }
}
