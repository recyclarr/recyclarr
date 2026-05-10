using Spectre.Console;

namespace Recyclarr.Cli.ErrorHandling;

internal class FatalErrorOutputStrategy(IAnsiConsole console, ILogger log) : IErrorOutputStrategy
{
    public void Write(IReadOnlyList<string> messages, Exception exception)
    {
        foreach (var message in messages)
        {
            console.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
            log.Error("{Message}", message);
        }

        log.Error(exception, "Exiting due to fatal error");
    }
}
