using Castle.Core.Internal;
using Recyclarr.Cli.Pipelines.Generic;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagPreviewPhase(IAnsiConsole console) : IPreviewPipelinePhase<TagPipelineContext>
{
    public void Execute(TagPipelineContext context)
    {
        var tagsToCreate = context.TransactionOutput;

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
