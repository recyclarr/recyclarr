using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;

internal class MediaManagementPreviewPhase(IAnsiConsole console, ISyncContextSource contextSource)
    : PreviewPipelinePhase<MediaManagementPipelineContext>(console, contextSource)
{
    protected override void RenderPreview(MediaManagementPipelineContext context)
    {
        RenderTitle(context);

        var table = new Table().AddColumns("[b]Field[/]", "[b]Value[/]");
        table.AddRow(
            "Download Propers and Repacks",
            context.TransactionOutput.DownloadPropersAndRepacks?.ToString() ?? "UNSET"
        );

        Console.Write(table);
    }
}
