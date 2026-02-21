using Recyclarr.Config.Parsing.ErrorHandling;
using Spectre.Console;

namespace Recyclarr.Cli.Processors;

internal static class ConfigFailureRenderer
{
    public static void Render(IAnsiConsole console, ILogger log, ConfigRegistryResult result)
    {
        if (result.Failures.Count == 0)
        {
            return;
        }

        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap().PadRight(1).PadLeft(0));
        grid.AddColumn(new GridColumn().PadLeft(0).PadRight(0));

        foreach (var failure in result.Failures)
        {
            var file = failure.FilePath?.Name ?? "unknown";
            var message = failure.ContextualMessage ?? failure.Message;

            grid.AddRow(
                new Markup("[red]•[/]"),
                new Markup($"[bold]{file.EscapeMarkup()}[/]: {message.EscapeMarkup()}")
            );

            log.Error(failure, "Config parsing failed in {File}: {Message}", file, message);
        }

        var panel = new Panel(
            new Rows(
                new Markup("[red]Errors[/]"),
                new Markup($"[red]{new string(c: '─', "Errors".Length)}[/]"),
                grid
            )
        )
            .Header("[bold]Config Diagnostics[/]")
            .Border(BoxBorder.Rounded)
            .Expand();

        console.WriteLine();
        console.Write(panel);
        console.WriteLine();
    }
}
