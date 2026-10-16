namespace Recyclarr.Analyzers.Tests;

internal sealed class RedundantDefaultArgumentAnalyzerTest
{
    private const string Declarations = """
        using System.Threading;

        static class Api
        {
            public static void Limit(int max = 10) { }
            public static void Pair(int a = 10, int b = 0) { }
            public static void Fetch(CancellationToken ct = default) { }
            public static void Required(CancellationToken ct) { }
            public static void Wide(long x = 0) { }
            public static void Overloaded(int a) { }
            public static void Overloaded(int a, int b = 0) { }
            public static void Nullable(int? x = 5) { }
        }

        class Base
        {
            public Base(int a, int b = 0) { }
        }

        """;

    private static Task<IReadOnlyList<string>> Analyze(string body, string extraTypes = "")
    {
        var source =
            Declarations
            + extraTypes
            + $$"""
                static class Caller
                {
                    static void Run(CancellationToken ct)
                    {
                        {{body}}
                    }
                }
                """;
        return AnalyzerRunner.FlaggedText(new RedundantDefaultArgumentAnalyzer(), source);
    }

    [Test]
    public async Task Reports_constant_equal_to_default()
    {
        var flagged = await Analyze("Api.Limit(10); Api.Wide(0); Api.Nullable(5);");

        flagged.Should().Equal("10", "0", "5");
    }

    [Test]
    public async Task Reports_constructor_calls_in_every_call_form()
    {
        var flagged = await Analyze(
            "Base explicitNew = new Base(1, 0); Base targetTyped = new(2, 0);",
            "class Derived : Base { public Derived() : base(3, 0) { } }\n"
        );

        // One each for `base(3, 0)`, `new Base(1, 0)`, and `new(2, 0)`
        flagged.Should().Equal("0", "0", "0");
    }

    [Test]
    public async Task Ignores_argument_whose_removal_selects_another_overload()
    {
        var flagged = await Analyze("Api.Overloaded(1, 0);");

        flagged.Should().BeEmpty();
    }

    [Test]
    public async Task Ignores_named_argument_equal_to_default()
    {
        var flagged = await Analyze("Api.Limit(max: 10); Api.Fetch(ct: default);");

        flagged.Should().BeEmpty();
    }

    [Test]
    public async Task Reports_default_and_none_for_optional_cancellation_token()
    {
        var flagged = await Analyze("Api.Fetch(default); Api.Fetch(CancellationToken.None);");

        flagged.Should().Equal("default", "CancellationToken.None");
    }

    [Test]
    public async Task Ignores_values_that_differ_from_default()
    {
        var flagged = await Analyze("Api.Limit(5); Api.Fetch(ct);");

        flagged.Should().BeEmpty();
    }

    [Test]
    public async Task Ignores_parameters_without_default()
    {
        var flagged = await Analyze("Api.Required(CancellationToken.None);");

        flagged.Should().BeEmpty();
    }

    [Test]
    public async Task Ignores_argument_followed_by_positional_argument()
    {
        var flagged = await Analyze("Api.Pair(10, 3); Api.Pair(10, b: 3);");

        flagged.Should().Equal("10");
    }
}
