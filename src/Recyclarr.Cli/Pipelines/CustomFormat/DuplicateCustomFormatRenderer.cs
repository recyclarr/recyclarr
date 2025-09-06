using Recyclarr.TrashGuide.CustomFormat;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class DuplicateCustomFormatRenderer(IAnsiConsole console)
{
    public void RenderDuplicates(IReadOnlyCollection<DuplicateCustomFormatInfo> duplicates)
    {
        if (duplicates.Count == 0)
        {
            return;
        }

        // Create tree widget for hierarchical display
        var tree = new Tree("[red]Resource Provider Conflicts[/]");

        foreach (var duplicate in duplicates)
        {
            // TrashId with associated names
            var namesList = string.Join(", ", duplicate.Names);
            var trashNode = tree.AddNode($"[yellow]{duplicate.TrashId}[/] - [white]{namesList}[/]");

            // Add sources under each TrashId
            foreach (var source in duplicate.Sources)
            {
                trashNode.AddNode($"[grey]• {source}[/]");
            }
        }

        // Add resolution guidance
        tree.AddNode(
            "[cyan]Resolution:[/] Remove duplicate repository configurations or choose different repositories."
        );

        // Create panel with tree inside
        var panel = new Panel(tree)
            .Header(new PanelHeader("[red bold]Duplicate Custom Formats Detected[/]"))
            .BorderColor(Color.Red)
            .RoundedBorder();

        console.Write(panel);
        console.WriteLine();
    }
}
