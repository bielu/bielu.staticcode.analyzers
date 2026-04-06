using Bielu.StaticCode.Analyzers.Analyzers;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class AsyncMethodNamingAnalyzerTests
{
    private readonly AsyncMethodNamingAnalyzer _analyzer = new();

    [Fact]
    public async Task AsyncMethodWithAsyncSuffix_ShouldNotReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public async Task DoWorkAsync() => await Task.CompletedTask;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task AsyncMethodWithoutAsyncSuffix_ShouldReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public async Task DoWork() => await Task.CompletedTask;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == AsyncMethodNamingAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task TaskReturningMethodWithoutAsync_ShouldReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public Task GetData() => Task.CompletedTask;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == AsyncMethodNamingAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task ValueTaskReturningMethodWithoutAsync_ShouldReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public ValueTask Process() => ValueTask.CompletedTask;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == AsyncMethodNamingAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task GenericTaskReturningMethodWithAsync_ShouldNotReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public Task<int> GetCountAsync() => Task.FromResult(42);
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task VoidReturningMethod_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService
            {
                public void DoWork() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task OverriddenMethod_ShouldNotReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public class Base
            {
                public virtual Task Execute() => Task.CompletedTask;
            }

            public class Derived : Base
            {
                public override Task Execute() => Task.CompletedTask;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        // Only the base method should be flagged (not the override)
        diagnostics.Length.ShouldBe(1);
    }

    [Fact]
    public async Task InterfaceImplementation_ShouldNotReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public interface IMyService
            {
                Task Execute();
            }

            public class MyService : IMyService
            {
                public Task Execute() => Task.CompletedTask;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        // Only the interface method itself should be flagged (not the implementation)
        diagnostics.Length.ShouldBe(1);
    }

    [Fact]
    public async Task GenericValueTaskWithAsyncSuffix_ShouldNotReportDiagnostic()
    {
        const string code = """
            using System.Threading.Tasks;

            public class MyService
            {
                public ValueTask<string> GetNameAsync() => ValueTask.FromResult("test");
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }
}
