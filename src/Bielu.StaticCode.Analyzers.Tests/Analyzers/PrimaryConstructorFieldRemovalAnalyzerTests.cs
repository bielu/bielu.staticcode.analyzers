using Bielu.StaticCode.Analyzers.Analyzers;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class PrimaryConstructorFieldRemovalAnalyzerTests
{
    private readonly PrimaryConstructorFieldRemovalAnalyzer _analyzer = new();

    [Fact]
    public async Task PrivateFieldAssignedFromPrimaryCtorParam_ShouldReportDiagnostic()
    {
        const string code = """
            public class MyService(string name)
            {
                private readonly string _name = name;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == PrimaryConstructorFieldRemovalAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task MultipleFieldsAssignedFromPrimaryCtorParams_ShouldReportDiagnostics()
    {
        const string code = """
            public class MyService(string name, int count)
            {
                private readonly string _name = name;
                private readonly int _count = count;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.Length.ShouldBe(2);
        diagnostics.ShouldAllBe(d => d.Id == PrimaryConstructorFieldRemovalAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task FieldWithoutInitializer_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService(string name)
            {
                private string _name;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task FieldInitializedWithNonParameter_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService(string name)
            {
                private readonly string _value = "constant";
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task PublicFieldAssignedFromParam_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService(string name)
            {
                public readonly string Name = name;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProtectedFieldAssignedFromParam_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService(string name)
            {
                protected readonly string _name = name;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task InternalFieldAssignedFromParam_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService(string name)
            {
                internal readonly string _name = name;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClassWithoutPrimaryConstructor_ShouldNotReportDiagnostic()
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

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task FieldInitializedWithExpression_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService(string name)
            {
                private readonly string _upperName = name.ToUpper();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ParameterPassedToBaseClass_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class Base(string name) { }

            public class Derived(string name) : Base(name)
            {
                private readonly string _name = name;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task OneParamPassedToBase_OtherNot_ShouldReportOnlyNonBaseParam()
    {
        const string code = """
            public class Base(string name) { }

            public class Derived(string name, int count) : Base(name)
            {
                private readonly string _name = name;
                private readonly int _count = count;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.Length.ShouldBe(1);
        diagnostics[0].Id.ShouldBe(PrimaryConstructorFieldRemovalAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().ShouldContain("_count");
    }

    [Fact]
    public async Task ImplicitPrivateField_ShouldReportDiagnostic()
    {
        // Fields without any access modifier are private by default
        const string code = """
            public class MyService(string name)
            {
                readonly string _name = name;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == PrimaryConstructorFieldRemovalAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task NonReadonlyPrivateField_ShouldReportDiagnostic()
    {
        const string code = """
            public class MyService(string name)
            {
                private string _name = name;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == PrimaryConstructorFieldRemovalAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task EmptyPrimaryConstructor_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class MyService()
            {
                private readonly string _name = "test";
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task MultipleParamsPassedToBase_ShouldNotReportDiagnostic()
    {
        const string code = """
            public class Base(string name, int id) { }

            public class Derived(string name, int id) : Base(name, id)
            {
                private readonly string _name = name;
                private readonly int _id = id;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task FieldInitializedFromLocalVariable_NotParam_ShouldNotReportDiagnostic()
    {
        // The initializer uses an identifier that doesn't match any primary ctor parameter
        const string code = """
            public class MyService(string name)
            {
                private static readonly string Default = "default";
                private readonly string _value = Default;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task DiagnosticMessageContainsFieldAndParamNames()
    {
        const string code = """
            public class MyService(string name)
            {
                private readonly string _name = name;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        var message = diagnostics[0].GetMessage();
        message.ShouldContain("_name");
        message.ShouldContain("name");
    }

    [Fact]
    public async Task BaseClassWithTransformedArgument_FieldShouldBeReported()
    {
        // The base class argument is a transformation of the parameter, not a simple pass-through.
        // The field storing the raw parameter should still be flagged.
        const string code = """
            public class Base(string name) { }

            public class Derived(string name) : Base(name.ToUpper())
            {
                private readonly string _name = name;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == PrimaryConstructorFieldRemovalAnalyzer.DiagnosticId);
    }
}
