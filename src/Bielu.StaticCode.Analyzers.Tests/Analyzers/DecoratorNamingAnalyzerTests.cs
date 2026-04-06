using Bielu.StaticCode.Analyzers.Analyzers;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class DecoratorNamingAnalyzerTests
{
    private readonly DecoratorNamingAnalyzer _analyzer = new();

    [Fact]
    public async Task ClassWithoutDecoratorPattern_ShouldNotReportDiagnostic()
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
    public async Task ClassWithCorrectDecoratorName_ShouldNotReportDiagnostic()
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

    [Fact]
    public async Task DecoratorWithWrongName_ShouldReportDiagnostic()
    {
        const string code = """
            public interface IApiService { void Execute(); }

            public class ApiServiceCache : IApiService
            {
                private readonly IApiService _inner;
                public ApiServiceCache(IApiService inner) { _inner = inner; }
                public void Execute() { _inner.Execute(); }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == DecoratorNamingAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task DecoratorWithModifierAndCorrectSuffix_ShouldNotReportDiagnostic()
    {
        const string code = """
            public interface IUserRepository { }

            public class LoggedUserRepositoryDecorator : IUserRepository
            {
                private readonly IUserRepository _inner;
                public LoggedUserRepositoryDecorator(IUserRepository inner) { _inner = inner; }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClassImplementingInterfaceWithoutDecoratorParameter_ShouldNotReportDiagnostic()
    {
        const string code = """
            public interface IApiService { void Execute(); }

            // Implements the interface but does NOT take IApiService as parameter → not a decorator
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
    public async Task DecoratorUsingPrimaryConstructor_WithWrongName_ShouldReportDiagnostic()
    {
        const string code = """
            public interface IApiService { void Execute(); }

            public class ApiServiceWrapper(IApiService inner) : IApiService
            {
                public void Execute() => inner.Execute();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == DecoratorNamingAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task DecoratorUsingPrimaryConstructor_WithCorrectName_ShouldNotReportDiagnostic()
    {
        const string code = """
            public interface IApiService { void Execute(); }

            public class ResilienceApiServiceDecorator(IApiService inner) : IApiService
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

            public abstract class BaseApiServiceDecorator : IApiService
            {
                private readonly IApiService _inner;
                protected BaseApiServiceDecorator(IApiService inner) { _inner = inner; }
                public abstract void Execute();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }
}
