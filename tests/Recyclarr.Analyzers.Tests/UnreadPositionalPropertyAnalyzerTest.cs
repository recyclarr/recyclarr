namespace Recyclarr.Analyzers.Tests;

internal sealed class UnreadPositionalPropertyAnalyzerTest
{
    private static Task<IReadOnlyList<string>> Analyze(string record, string body)
    {
        var source = $$"""
            using System.Collections.Generic;

            interface IHasMin
            {
                int Min { get; }
            }

            class Owner
            {
                {{record}}

                static object Run()
                {
                    {{body}}
                }
            }
            """;
        return AnalyzerRunner.FlaggedText(new UnreadPositionalPropertyAnalyzer(), source);
    }

    [Test]
    public async Task Reports_properties_used_only_by_generated_members()
    {
        var flagged = await Analyze(
            "private sealed record Size(int Min, int Max);",
            "return new Size(1, 2) == new Size(1, 2);"
        );

        flagged.Should().Equal("Min", "Max");
    }

    [Test]
    public async Task Reports_property_only_written()
    {
        var flagged = await Analyze(
            "private sealed record Size(int Min, int Max);",
            "var size = new Size(1, 2) with { Min = 3 }; return size.Max;"
        );

        flagged.Should().Equal("Min");
    }

    [TestCase("var size = new Size(1, 2); return size.Min + size.Max;")]
    [TestCase("var (min, max) = new Size(1, 2); return min + max;")]
    [TestCase("var ((min, max), n) = (new Size(1, 2), 3); return min + max + n;")]
    [TestCase("new Size(1, 2).Deconstruct(out var min, out var max); return min + max;")]
    [TestCase(
        "var t = 0; foreach (var ((min, max), n) in new List<(Size, int)>()) { t += min; } return t;"
    )]
    [TestCase("return new Size(1, 2) is (1, _);")]
    [TestCase(
        "var total = 0; foreach (var (min, max) in new List<Size>()) { total += min; } return total;"
    )]
    [TestCase(
        "return (System.Func<IAsyncEnumerable<Size>, System.Threading.Tasks.Task<int>>)Sum; "
            + "async System.Threading.Tasks.Task<int> Sum(IAsyncEnumerable<Size> xs) { var t = 0; "
            + "await foreach (var (min, max) in xs) { t += min + max; } return t; }"
    )]
    public async Task Ignores_properties_that_code_reads(string body)
    {
        var flagged = await Analyze("private sealed record Size(int Min, int Max);", body);

        flagged.Should().BeEmpty();
    }

    [Test]
    public async Task Ignores_records_visible_outside_private_scope()
    {
        var flagged = await Analyze(
            "internal sealed record Size(int Min, int Max);",
            "return new Size(1, 2);"
        );

        flagged.Should().BeEmpty();
    }

    [Test]
    public async Task Ignores_interface_implementations()
    {
        var flagged = await Analyze(
            "private sealed record Size(int Min, int Max) : IHasMin;",
            "var size = new Size(1, 2); return size.Max;"
        );

        flagged.Should().BeEmpty();
    }
}
