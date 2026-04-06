using Bielu.StaticCode.Analyzers.Analyzers;
using Shouldly;
using Xunit;

namespace Bielu.StaticCode.Analyzers.Tests.Analyzers;

public class NullGuardClauseAnalyzerTests
{
    private readonly NullGuardClauseAnalyzer _analyzer = new();

    [Fact]
    public async Task ManualNullCheckWithEqualsNull_ShouldReportDiagnostic()
    {
        const string code = """
            using System;

            public class MyService
            {
                public void DoWork(string name)
                {
                    if (name == null)
                        throw new ArgumentNullException(nameof(name));
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == NullGuardClauseAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task ManualNullCheckWithIsNull_ShouldReportDiagnostic()
    {
        const string code = """
            using System;

            public class MyService
            {
                public void DoWork(string name)
                {
                    if (name is null)
                        throw new ArgumentNullException(nameof(name));
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == NullGuardClauseAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task ManualNullCheckWithBlock_ShouldReportDiagnostic()
    {
        const string code = """
            using System;

            public class MyService
            {
                public void DoWork(string name)
                {
                    if (name == null)
                    {
                        throw new ArgumentNullException(nameof(name));
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == NullGuardClauseAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task ManualNullCheckReversed_ShouldReportDiagnostic()
    {
        const string code = """
            using System;

            public class MyService
            {
                public void DoWork(string name)
                {
                    if (null == name)
                        throw new ArgumentNullException(nameof(name));
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldContain(d => d.Id == NullGuardClauseAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task ThrowIfNull_ShouldNotReportDiagnostic()
    {
        const string code = """
            using System;

            public class MyService
            {
                public void DoWork(string name)
                {
                    ArgumentNullException.ThrowIfNull(name);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task NullCheckWithElse_ShouldNotReportDiagnostic()
    {
        const string code = """
            using System;

            public class MyService
            {
                public string DoWork(string name)
                {
                    if (name == null)
                        throw new ArgumentNullException(nameof(name));
                    else
                        return name;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        // Has an else clause — not a simple guard clause
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task NullCheckWithNonArgumentNullException_ShouldNotReportDiagnostic()
    {
        const string code = """
            using System;

            public class MyService
            {
                public void DoWork(string name)
                {
                    if (name == null)
                        throw new InvalidOperationException("name was null");
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(_analyzer, code);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task NullCoalescingThrow_ShouldNotReportDiagnostic()
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

        // This is a different pattern (null-coalescing), not an if-throw guard
        diagnostics.ShouldBeEmpty();
    }
}
