using Spectre.Console;

namespace Recyclarr.Cli.ErrorHandling;

internal class FatalErrorOutputStrategy(ILogger log, IAnsiConsole console) : IErrorOutputStrategy
{
    public void WriteError(string message)
    {
        log.Error("{Message}", message);
        console.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
    }
}
