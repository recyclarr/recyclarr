using Spectre.Console;

namespace Recyclarr.Cli.ErrorHandling;

internal class FatalErrorOutputStrategy(IAnsiConsole console) : IErrorOutputStrategy
{
    public void WriteError(string message)
    {
        console.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
    }
}
