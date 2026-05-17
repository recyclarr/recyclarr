using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Preview;

internal static class RadarrNamingPreviewRenderer
{
    public static void Render(IAnsiConsole console, RadarrNamingSyncResult result)
    {
        var data = result.Desired;
        var table = new Table().AddColumns("[b]Field[/]", "[b]Value[/]");
        AddRow(table, "Enable Movie Renames?", data.RenameMovies);
        AddRow(table, "Movie", data.StandardMovieFormat);
        AddRow(table, "Folder", data.MovieFolderFormat);
        console.Write(table);
    }

    private static void AddRow(Table table, string field, object? value)
    {
        table.AddRow(field.EscapeMarkup(), value?.ToString().EscapeMarkup() ?? "UNSET");
    }
}
