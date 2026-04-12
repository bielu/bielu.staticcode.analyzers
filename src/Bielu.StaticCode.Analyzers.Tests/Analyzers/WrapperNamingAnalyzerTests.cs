using Bielu.StaticCode.Analyzers.Analyzers;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class WrapperNamingAnalyzerTests
{
    private readonly WrapperNamingAnalyzer _analyzer = new();

    [Fact]
    public async Task ClassWithoutWrapperPattern_ShouldNotReportDiagnostic()
    {
        const string code = """
            public interface IApiService { void Execute(); }

            public class ApiService : IApiService
            {
                public void Execute() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClassWithCorrectWrapperName_ShouldNotReportDiagnostic()
    {
        const string code = """
            public interface IApiService { void Execute(); }

            public class RetryApiServiceWrapper : IApiService
            {
                private readonly IApiService _inner;
                public RetryApiServiceWrapper(IApiService inner) { _inner = inner; }
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
            public interface IApiService { void Execute(); }

            public class ApiServiceProxy : IApiService
            {
                private readonly IApiService _inner;
                public ApiServiceProxy(IApiService inner) { _inner = inner; }
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
            public interface IUserRepository { }

            public class CachedUserRepositoryWrapper : IUserRepository
            {
                private readonly IUserRepository _inner;
                public CachedUserRepositoryWrapper(IUserRepository inner) { _inner = inner; }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClassImplementingInterfaceWithoutWrapperParameter_ShouldNotReportDiagnostic()
    {
        const string code = """
            public interface IApiService { void Execute(); }

            public class CompositeApiService : IApiService
            {
                public CompositeApiService(string connectionString) { }
                public void Execute() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task WrapperUsingPrimaryConstructor_WithWrongName_ShouldReportDiagnostic()
    {
        const string code = """
            public interface IApiService { void Execute(); }

            public class ApiServiceFacade(IApiService inner) : IApiService
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
            public interface IApiService { void Execute(); }

            public class ResilienceApiServiceWrapper(IApiService inner) : IApiService
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
            public interface IApiService { void Execute(); }

            public abstract class BaseApiServiceWrapper : IApiService
            {
                private readonly IApiService _inner;
                protected BaseApiServiceWrapper(IApiService inner) { _inner = inner; }
                public abstract void Execute();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task DecoratorNamedClass_ShouldNotReportWrapperDiagnostic()
    {
        const string code = """
            public interface IApiService { void Execute(); }

            public class CachedApiServiceDecorator : IApiService
            {
                private readonly IApiService _inner;
                public CachedApiServiceDecorator(IApiService inner) { _inner = inner; }
                public void Execute() { _inner.Execute(); }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }
}
