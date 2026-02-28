using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingPreviewPhase(IAnsiConsole console)
    : PreviewPipelinePhase<SonarrNamingPipelineContext>(console)
{
    protected override void RenderPreview(SonarrNamingPipelineContext context)
    {
        RenderTitle(context);

        var dto = context.TransactionOutput;
        var table = new Table().AddColumns("[b]Field[/]", "[b]Value[/]");
        AddRow(table, "Enable Episode Renames?", dto.RenameEpisodes);
        AddRow(table, "Series Folder", dto.SeriesFolderFormat);
        AddRow(table, "Season Folder", dto.SeasonFolderFormat);
        AddRow(table, "Standard Episodes", dto.StandardEpisodeFormat);
        AddRow(table, "Daily Episodes", dto.DailyEpisodeFormat);
        AddRow(table, "Anime Episodes", dto.AnimeEpisodeFormat);
        Console.Write(table);
    }

    private static void AddRow(Table table, string field, object? value)
    {
        table.AddRow(field.EscapeMarkup(), value?.ToString().EscapeMarkup() ?? "UNSET");
    }
}
