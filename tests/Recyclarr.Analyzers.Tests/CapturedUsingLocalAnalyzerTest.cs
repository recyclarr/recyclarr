namespace Recyclarr.Analyzers.Tests;

internal sealed class CapturedUsingLocalAnalyzerTest
{
    private static Task<IReadOnlyList<string>> Analyze(string body)
    {
        var source = $$"""
            using System;
            using AwesomeAssertions;

            sealed class Scope : IDisposable
            {
                public int Resolve() => 1;
                public void Dispose() { }
            }

            static class Caller
            {
                static Func<int> Run()
                {
                    {{body}}
                }
            }
            """;
        return AnalyzerRunner.FlaggedText(new CapturedUsingLocalAnalyzer(), source);
    }

    [Test]
    public async Task Reports_using_declaration_captured_by_lambda()
    {
        var flagged = await Analyze("using var scope = new Scope(); return () => scope.Resolve();");

        flagged.Should().Equal("scope");
    }

    [Test]
    public async Task Reports_using_statement_variable_captured_by_lambda()
    {
        var flagged = await Analyze(
            "using (var scope = new Scope()) { return () => scope.Resolve(); }"
        );

        flagged.Should().Equal("scope");
    }

    [Test]
    public async Task Ignores_lambda_local_used_only_as_assertion_subject()
    {
        var flagged = await Analyze(
            "using var scope = new Scope(); var act = () => scope.Resolve(); "
                + "act.Should().NotThrow(); return () => 0;"
        );

        flagged.Should().BeEmpty();
    }

    [Test]
    public async Task Reports_asserted_lambda_local_that_also_escapes()
    {
        var flagged = await Analyze(
            "using var scope = new Scope(); Func<int> act = () => scope.Resolve(); "
                + "act.Should().NotThrow(); return act;"
        );

        flagged.Should().Equal("scope");
    }

    [Test]
    public async Task Ignores_captured_local_without_using()
    {
        var flagged = await Analyze("var scope = new Scope(); return () => scope.Resolve();");

        flagged.Should().BeEmpty();
    }

    [Test]
    public async Task Ignores_using_local_outside_lambda()
    {
        var flagged = await Analyze(
            "using var scope = new Scope(); var value = scope.Resolve(); return () => value;"
        );

        flagged.Should().BeEmpty();
    }

    [Test]
    public async Task Ignores_using_local_declared_inside_lambda()
    {
        var flagged = await Analyze(
            "return () => { using var scope = new Scope(); return scope.Resolve(); };"
        );

        flagged.Should().BeEmpty();
    }
}
