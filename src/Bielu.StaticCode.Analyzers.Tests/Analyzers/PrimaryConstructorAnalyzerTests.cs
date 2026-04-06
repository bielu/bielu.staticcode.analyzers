using Bielu.StaticCode.Analyzers.Analyzers;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class PrimaryConstructorAnalyzerTests
{
    private readonly PrimaryConstructorAnalyzer _analyzer = new();

    [Fact]
    public async Task ClassAlreadyUsingPrimaryConstructor_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService(string name)
            {
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClassWithSingleConstructorOnlyAssignments_ShouldReportDiagnostic()
    {
        const string code = """
            public class MyService
            {
                private readonly string _name;

                public MyService(string name)
                {
                    _name = name;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == PrimaryConstructorAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task ClassWithThisDotAssignment_ShouldReportDiagnostic()
    {
        const string code = """
            public class MyService
            {
                public string Name { get; }

                public MyService(string name)
                {
                    this.Name = name;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == PrimaryConstructorAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task ClassWithEmptyConstructor_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService
            {
                public MyService()
                {
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClassWithMultipleConstructors_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService
            {
                private readonly string _name;

                public MyService(string name)
                {
                    _name = name;
                }

                public MyService() : this("default")
                {
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ConstructorWithComplexLogic_ShouldNotReportDiagnostic()
    {
        const string code = """
            using System;

            public class MyService
            {
                private readonly string _name;

                public MyService(string name)
                {
                    _name = name ?? throw new ArgumentNullException(nameof(name));
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ConstructorWithBaseInitializer_ShouldNotReportDiagnostic()
    {
        // Base already uses a primary constructor; MyService chains to it via : base(...)
        // The analyzer should NOT flag MyService because it has a base() initializer
        const string code = """
            public class Base(string name) { }

            public class MyService : Base
            {
                public MyService(string name) : base(name)
                {
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task AbstractClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            public abstract class BaseService
            {
                private readonly string _name;

                protected BaseService(string name)
                {
                    _name = name;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task StaticClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            public static class MyHelper
            {
                private static string? _value;

                static MyHelper()
                {
                    _value = "test";
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task MultipleFieldAssignments_ShouldReportDiagnostic()
    {
        const string code = """
            public class MyService
            {
                private readonly string _name;
                private readonly int _count;

                public MyService(string name, int count)
                {
                    _name = name;
                    _count = count;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == PrimaryConstructorAnalyzer.DiagnosticId);
    }
}
