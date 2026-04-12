using Bielu.StaticCode.Analyzers.Analyzers;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class WrapperNamingAnalyzerTests
{
    private readonly WrapperNamingAnalyzer _analyzer = new();

    [Fact]
    public async Task ClassNotWrappingAnything_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class ApiService
            {
                public void Execute() { }
            }

            public class MyController
            {
                private readonly ApiService _svc;
                public MyController(ApiService svc) { _svc = svc; }
                public void HandleRequest() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task WrapperWithCorrectName_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class ApiService
            {
                public void Execute() { }
            }

            public class RetryApiServiceWrapper
            {
                private readonly ApiService _inner;
                public RetryApiServiceWrapper(ApiService inner) { _inner = inner; }
                public void Execute() { _inner.Execute(); }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task WrapperWithWrongName_ShouldReportDiagnostic()
    {
        const string code = """
            public class ApiService
            {
                public void Execute() { }
            }

            public class ApiServiceProxy
            {
                private readonly ApiService _inner;
                public ApiServiceProxy(ApiService inner) { _inner = inner; }
                public void Execute() { _inner.Execute(); }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == WrapperNamingAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task WrapperWithModifierAndCorrectSuffix_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class UserRepository
            {
                public void Save() { }
            }

            public class CachedUserRepositoryWrapper
            {
                private readonly UserRepository _inner;
                public CachedUserRepositoryWrapper(UserRepository inner) { _inner = inner; }
                public void Save() { _inner.Save(); }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClassWithNoMatchingMethods_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class ApiService
            {
                public void Execute() { }
            }

            public class ApiServiceHelper
            {
                private readonly ApiService _svc;
                public ApiServiceHelper(ApiService svc) { _svc = svc; }
                public void DoSomethingElse() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task WrapperUsingPrimaryConstructor_WithWrongName_ShouldReportDiagnostic()
    {
        const string code = """
            public class ApiService
            {
                public void Execute() { }
            }

            public class ApiServiceFacade(ApiService inner)
            {
                public void Execute() => inner.Execute();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == WrapperNamingAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task WrapperUsingPrimaryConstructor_WithCorrectName_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class ApiService
            {
                public void Execute() { }
            }

            public class ResilienceApiServiceWrapper(ApiService inner)
            {
                public void Execute() => inner.Execute();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task AbstractClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class ApiService
            {
                public void Execute() { }
            }

            public abstract class BaseApiServiceWrapper
            {
                private readonly ApiService _inner;
                protected BaseApiServiceWrapper(ApiService inner) { _inner = inner; }
                public abstract void Execute();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task InheritedClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class ApiService
            {
                public void Execute() { }
            }

            public class ExtendedApiService : ApiService
            {
                public ExtendedApiService(ApiService inner) { }
                public void ExtraMethod() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClassTakingInterface_ShouldNotReportDiagnostic()
    {
        const string code = """
            public interface IApiService { void Execute(); }

            public class ApiServiceProxy : IApiService
            {
                private readonly IApiService _inner;
                public ApiServiceProxy(IApiService inner) { _inner = inner; }
                public void Execute() { _inner.Execute(); }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        // Interface-based wrapping is the decorator pattern, not wrapper
        diagnostics.ShouldBeEmpty();
    }
}
