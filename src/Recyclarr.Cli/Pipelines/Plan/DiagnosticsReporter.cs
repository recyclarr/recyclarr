using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Pipelines.Plan;

internal class DiagnosticsReporter(IAnsiConsole console)
{
    public void Report(PlanDiagnostics diagnostics)
    {
        var sections = new List<IRenderable>();

        // Errors section
        var errors = BuildErrorsList(diagnostics);
        if (errors.Count > 0)
        {
            sections.Add(BuildSection("[red]Errors[/]", errors, "red"));
        }

        // Warnings section
        var warnings = BuildWarningsList(diagnostics);
        if (warnings.Count > 0)
        {
            sections.Add(BuildSection("[yellow]Warnings[/]", warnings, "yellow"));
        }

        if (sections.Count == 0)
        {
            return;
        }

        var panel = new Panel(new Rows(sections))
            .Header("[bold]Configuration Diagnostics[/]")
            .Border(BoxBorder.Rounded)
            .Expand();

        console.Write(panel);
    }

    private static List<string> BuildErrorsList(PlanDiagnostics diagnostics)
    {
        return diagnostics
            .InvalidNamingFormats.Select(x => $"Invalid {x.Type} naming format: {x.ConfigValue}")
            .Concat(diagnostics.Errors)
            .ToList();
    }

    private static List<string> BuildWarningsList(PlanDiagnostics diagnostics)
    {
        return diagnostics
            .InvalidTrashIds.Select(x => $"Invalid trash_id: {x}")
            .Concat(diagnostics.Warnings)
            .ToList();
    }

    private static Rows BuildSection(string header, List<string> items, string bulletColor)
    {
        var grid = items.Aggregate(
            new Grid().AddColumn().AddColumn(),
            (g, item) =>
                g.AddRow(new Markup($"  [{bulletColor}]•[/]"), new Markup(item.EscapeMarkup()))
        );

        return new Rows(new Markup(header), grid);
    }
}
