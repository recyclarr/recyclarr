using Castle.Core.Internal;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagPreviewPhase(IAnsiConsole console)
{
    public void Execute(IReadOnlyList<string> tagsToCreate)
    {
        if (tagsToCreate.IsNullOrEmpty())
        {
            console.WriteLine();
            console.MarkupLine("[green]No tags to create[/]");
            console.WriteLine();
            return;
        }

        var table = new Table {Border = TableBorder.Simple};
        table.AddColumn("[olive]Tags To Create[/]");

        foreach (var tag in tagsToCreate)
        {
            table.AddRow(tag);
        }

        console.Write(table);
    }
}
