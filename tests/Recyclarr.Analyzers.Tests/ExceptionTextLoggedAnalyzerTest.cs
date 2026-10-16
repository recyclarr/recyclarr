namespace Recyclarr.Analyzers.Tests;

internal sealed class ExceptionTextLoggedAnalyzerTest
{
    private static Task<IReadOnlyList<string>> Analyze(string catchBody)
    {
        var source = $$"""
            using System;
            using Serilog;

            static class Caller
            {
                static void Run(ILogger log, Action work)
                {
                    try
                    {
                        work();
                    }
                    catch (InvalidOperationException e)
                    {
                        {{catchBody}}
                    }
                }
            }
            """;
        return AnalyzerRunner.FlaggedText(new ExceptionTextLoggedAnalyzer(), source);
    }

    [Test]
    public async Task Reports_catch_variable_message_in_template_arguments()
    {
        var flagged = await Analyze("log.Debug(\"Failed {Item}: {Error}\", \"x\", e.Message);");

        flagged.Should().Equal("e.Message");
    }

    [Test]
    public async Task Ignores_exception_passed_to_exception_parameter()
    {
        var flagged = await Analyze("log.Debug(e, \"Failed {Item}\", \"x\");");

        flagged.Should().BeEmpty();
    }

    [Test]
    public async Task Ignores_message_used_outside_log_call()
    {
        var flagged = await Analyze("Console.WriteLine(e.Message);");

        flagged.Should().BeEmpty();
    }

    [Test]
    public async Task Ignores_message_of_exception_not_caught()
    {
        var flagged = await Analyze(
            "var other = new Exception(); log.Debug(\"Other: {Error}\", other.Message);"
        );

        flagged.Should().BeEmpty();
    }
}
