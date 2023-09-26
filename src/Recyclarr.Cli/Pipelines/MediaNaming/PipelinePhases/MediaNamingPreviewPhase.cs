using Recyclarr.ServarrApi.MediaNaming;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingPreviewPhase
{
    private readonly IAnsiConsole _console;
    private Table? _table;

    public MediaNamingPreviewPhase(IAnsiConsole console)
    {
        _console = console;
    }

    public void Execute(MediaNamingDto serviceDto)
    {
        _table = new Table()
            .Title("Media Naming [red](Preview)[/]")
            .AddColumns("[b]Field[/]", "[b]Value[/]");

        switch (serviceDto)
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

        _console.WriteLine();
        _console.Write(_table);
    }

    private void AddRow(string field, object? value)
    {
        _table?.AddRow(field, value?.ToString() ?? "UNSET");
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
