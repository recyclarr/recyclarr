using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

internal class MediaNamingPreviewPhase(IAnsiConsole console, ISyncContextSource contextSource)
    : PreviewPipelinePhase<MediaNamingPipelineContext>(console, contextSource)
{
    private Table? _table;

    protected override void RenderPreview(MediaNamingPipelineContext context)
    {
        RenderTitle(context);

        _table = new Table().AddColumns("[b]Field[/]", "[b]Value[/]");

        switch (context.TransactionOutput)
        {
            case RadarrMediaNamingDto dto:
                PreviewRadarr(dto);
                break;

            case SonarrMediaNamingDto dto:
                PreviewSonarr(dto);
                break;

            default:
                throw new ArgumentException("Config type not supported in media naming preview");
        }

        Console.Write(_table);
    }

    private void AddRow(string field, object? value)
    {
        _table?.AddRow(field.EscapeMarkup(), value?.ToString().EscapeMarkup() ?? "UNSET");
    }

    private void PreviewRadarr(RadarrMediaNamingDto dto)
    {
        AddRow("Enable Movie Renames?", dto.RenameMovies);
        AddRow("Movie", dto.StandardMovieFormat);
        AddRow("Folder", dto.MovieFolderFormat);
    }

    private void PreviewSonarr(SonarrMediaNamingDto dto)
    {
        AddRow("Enable Episode Renames?", dto.RenameEpisodes);
        AddRow("Series Folder", dto.SeriesFolderFormat);
        AddRow("Season Folder", dto.SeasonFolderFormat);
        AddRow("Standard Episodes", dto.StandardEpisodeFormat);
        AddRow("Daily Episodes", dto.DailyEpisodeFormat);
        AddRow("Anime Episodes", dto.AnimeEpisodeFormat);
    }
}
