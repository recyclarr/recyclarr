using Recyclarr.Client.V1;
using Spectre.Console;

namespace Recyclarr.Cli.Preview;

internal static class MediaManagementPreviewRenderer
{
    public static void Render(IAnsiConsole console, MediaManagementResultResponse result)
    {
        var table = new Table().AddColumns("[b]Field[/]", "[b]Value[/]");
        table.AddRow("Download Propers and Repacks", result.PropersAndRepacks ?? "UNSET");
        console.Write(table);
    }
}
