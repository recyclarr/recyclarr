using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatPreviewPhase(IAnsiConsole console, ISyncContextSource contextSource)
    : PreviewPipelinePhase<CustomFormatPipelineContext>(console, contextSource)
{
    protected override void RenderPreview(CustomFormatPipelineContext context)
    {
        RenderTitle(context);

        var transactions = context.TransactionOutput;
        var hasChanges = transactions.TotalCustomFormatChanges > 0;

        if (!hasChanges)
        {
            Console.MarkupLine("[dim]No changes[/]");
            return;
        }

        var table = new Table()
            .AddColumn("[bold]Action[/]")
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Trash ID[/]");

        foreach (var cf in transactions.NewCustomFormats)
        {
            AddRow(table, "[green]Create[/]", cf.Name, cf.TrashId);
        }

        foreach (var cf in transactions.UpdatedCustomFormats)
        {
            AddRow(table, "[yellow]Update[/]", cf.Name, cf.TrashId);
        }

        foreach (var cf in transactions.DeletedCustomFormats)
        {
            AddRow(table, "[red]Delete[/]", cf.Name, cf.TrashId);
        }

        Console.Write(table);

        var unchanged = transactions.UnchangedCustomFormats.Count;
        if (unchanged > 0)
        {
            Console.MarkupLine($"[dim]Unchanged: {unchanged}[/]");
        }

        return;

        static void AddRow(Table t, string action, string name, string trashId)
        {
            t.AddRow(action, name.EscapeMarkup(), $"[dim]{trashId}[/]");
        }
    }
}
