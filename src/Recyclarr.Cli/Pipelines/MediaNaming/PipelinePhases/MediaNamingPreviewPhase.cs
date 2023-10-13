using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.MediaNaming;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingPreviewPhase(IAnsiConsole console) : IPreviewPipelinePhase<MediaNamingPipelineContext>
{
    private Table? _table;

    public void Execute(MediaNamingPipelineContext context)
    {
        _table = new Table()
            .Title("Media Naming [red](Preview)[/]")
            .AddColumns("[b]Field[/]", "[b]Value[/]");

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

        console.WriteLine();
        console.Write(_table);
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
