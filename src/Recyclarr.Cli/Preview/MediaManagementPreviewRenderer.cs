using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Preview;

internal static class MediaManagementPreviewRenderer
{
    public static void Render(IAnsiConsole console, MediaManagementSyncResult result)
    {
        var table = new Table().AddColumns("[b]Field[/]", "[b]Value[/]");
        table.AddRow(
            "Download Propers and Repacks",
            result.Desired.PropersAndRepacks?.ToString() ?? "UNSET"
        );
        console.Write(table);
    }
}
