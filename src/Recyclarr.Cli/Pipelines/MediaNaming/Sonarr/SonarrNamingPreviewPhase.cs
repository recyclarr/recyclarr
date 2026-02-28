using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingPreviewPhase(IAnsiConsole console)
    : PreviewPipelinePhase<SonarrNamingPipelineContext>(console)
{
    protected override void RenderPreview(SonarrNamingPipelineContext context)
    {
        RenderTitle(context);

        var data = context.TransactionOutput;
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
