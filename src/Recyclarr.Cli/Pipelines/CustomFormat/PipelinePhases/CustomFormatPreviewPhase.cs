using Recyclarr.Sync;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatPreviewPhase(IAnsiConsole console, ISyncContextSource contextSource)
    : PreviewPipelinePhase<CustomFormatPipelineContext>(console, contextSource)
{
    protected override void RenderPreview(CustomFormatPipelineContext context)
    {
        RenderTitle(context);

        var transactions = context.TransactionOutput;

        if (transactions.TotalCustomFormatChanges == 0)
        {
            Console.MarkupLine("[dim]No changes[/]");
            return;
        }

        string GetSource(string trashId) =>
            context.Plan.GetCustomFormat(trashId)?.GroupName ?? "(ungrouped)";

        // Build tuples for all changes
        var allChanges = transactions
            .NewCustomFormats.Select(cf =>
                (
                    Source: GetSource(cf.TrashId),
                    Action: "Create",
                    Color: "green",
                    cf.Name,
                    cf.TrashId
                )
            )
            .Concat(
                transactions.UpdatedCustomFormats.Select(cf =>
                    (
                        Source: GetSource(cf.TrashId),
                        Action: "Update",
                        Color: "yellow",
                        cf.Name,
                        cf.TrashId
                    )
                )
            )
            .Concat(
                transactions.DeletedCustomFormats.Select(m =>
                    (
                        Source: GetSource(m.TrashId),
                        Action: "Delete",
                        Color: "red",
                        m.Name,
                        m.TrashId
                    )
                )
            )
            .GroupBy(x => x.Source)
            .OrderBy(g => g.Key == "(ungrouped)" ? 0 : 1)
            .ThenBy(g => g.Key);

        var tree = new Tree("[bold]Changes[/]");

        foreach (var sourceGroup in allChanges)
        {
            var table = new Table()
                .AddColumn("[bold]Action[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Trash ID[/]");

            foreach (var (_, action, color, name, trashId) in sourceGroup)
            {
                table.AddRow($"[{color}]{action}[/]", name.EscapeMarkup(), $"[dim]{trashId}[/]");
            }

            tree.AddNode(new Rows(new Markup($"[dim]{sourceGroup.Key.EscapeMarkup()}[/]"), table));
        }

        Console.Write(tree);

        var unchanged = transactions.UnchangedCustomFormats.Count;
        if (unchanged > 0)
        {
            Console.MarkupLine($"[dim]Unchanged: {unchanged}[/]");
        }
    }
}
