using Recyclarr.Servarr.MediaManagement;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;

internal class MediaManagementPreviewRenderer(IAnsiConsole console)
    : PreviewRenderer<MediaManagementData>(console)
{
    protected override void RenderData(MediaManagementData data)
    {
        var table = new Table().AddColumns("[b]Field[/]", "[b]Value[/]");
        table.AddRow("Download Propers and Repacks", data.PropersAndRepacks?.ToString() ?? "UNSET");

        Console.Write(table);
    }
}
