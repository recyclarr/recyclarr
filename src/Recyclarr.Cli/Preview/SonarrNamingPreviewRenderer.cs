using Recyclarr.Servarr.MediaNaming;
using Spectre.Console;

namespace Recyclarr.Cli.Preview;

internal class SonarrNamingPreviewRenderer(IAnsiConsole console)
    : PreviewRenderer<SonarrNamingData>(console)
{
    protected override void RenderData(SonarrNamingData data)
    {
        var table = new Table().AddColumns("[b]Field[/]", "[b]Value[/]");
        AddRow(table, "Enable Episode Renames?", data.RenameEpisodes);
        AddRow(table, "Series Folder", data.SeriesFolderFormat);
        AddRow(table, "Season Folder", data.SeasonFolderFormat);
        AddRow(table, "Standard Episodes", data.StandardEpisodeFormat);
        AddRow(table, "Daily Episodes", data.DailyEpisodeFormat);
        AddRow(table, "Anime Episodes", data.AnimeEpisodeFormat);
        Console.Write(table);
    }

    private static void AddRow(Table table, string field, object? value)
    {
        table.AddRow(field.EscapeMarkup(), value?.ToString().EscapeMarkup() ?? "UNSET");
    }
}
