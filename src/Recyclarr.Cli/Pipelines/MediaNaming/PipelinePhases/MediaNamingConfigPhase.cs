using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public record InvalidNamingConfig(string Type, string ConfigValue);

public record ProcessedNamingConfig
{
    public required MediaNamingDto Dto { get; init; }
    public IReadOnlyCollection<InvalidNamingConfig> InvalidNaming { get; init; } = new List<InvalidNamingConfig>();
}

public class MediaNamingConfigPhase
{
    private readonly IMediaNamingGuideService _guide;
    private readonly ISonarrCapabilityFetcher _sonarrCapabilities;
    private List<InvalidNamingConfig> _errors = new();

    public MediaNamingConfigPhase(IMediaNamingGuideService guide, ISonarrCapabilityFetcher sonarrCapabilities)
    {
        _guide = guide;
        _sonarrCapabilities = sonarrCapabilities;
    }

    public async Task<ProcessedNamingConfig> Execute(IServiceConfiguration config)
    {
        _errors = new List<InvalidNamingConfig>();

        var dto = config switch
        {
            RadarrConfiguration c => ProcessRadarrNaming(c),
            SonarrConfiguration c => await ProcessSonarrNaming(c),
            _ => throw new ArgumentException("Configuration type unsupported for naming sync")
        };

        return new ProcessedNamingConfig {Dto = dto, InvalidNaming = _errors};
    }

    private MediaNamingDto ProcessRadarrNaming(RadarrConfiguration config)
    {
        var guideData = _guide.GetRadarrNamingData();
        var configData = config.MediaNaming;

        return new RadarrMediaNamingDto
        {
            StandardMovieFormat = ObtainFormat(guideData.File, configData.Movie?.Format, "Movie File Format"),
            MovieFolderFormat = ObtainFormat(guideData.Folder, configData.Folder, "Movie Folder Format"),
            RenameMovies = configData.Movie?.Rename
        };
    }

    private async Task<MediaNamingDto> ProcessSonarrNaming(SonarrConfiguration config)
    {
        var guideData = _guide.GetSonarrNamingData();
        var configData = config.MediaNaming;
        var capabilities = await _sonarrCapabilities.GetCapabilities(config);
        var keySuffix = capabilities.SupportsCustomFormats ? ":4" : ":3";

        return new SonarrMediaNamingDto
        {
            SeasonFolderFormat = ObtainFormat(guideData.Season, configData.Season, "Season Folder Format"),
            SeriesFolderFormat = ObtainFormat(guideData.Series, configData.Series, "Series Folder Format"),
            StandardEpisodeFormat = ObtainFormat(
                guideData.Episodes.Standard,
                configData.Episodes?.Standard,
                keySuffix,
                "Standard Episode Format"),
            DailyEpisodeFormat = ObtainFormat(
                guideData.Episodes.Daily,
                configData.Episodes?.Daily,
                keySuffix,
                "Daily Episode Format"),
            AnimeEpisodeFormat = ObtainFormat(
                guideData.Episodes.Anime,
                configData.Episodes?.Anime,
                keySuffix,
                "Anime Episode Format"),
            RenameEpisodes = configData.Episodes?.Rename
        };
    }

    private string? ObtainFormat(
        IReadOnlyDictionary<string, string> guideFormats,
        string? configFormatKey,
        string errorDescription)
    {
        return ObtainFormat(guideFormats, configFormatKey, null, errorDescription);
    }

    private string? ObtainFormat(
        IReadOnlyDictionary<string, string> guideFormats,
        string? configFormatKey,
        string? keySuffix,
        string errorDescription)
    {
        if (configFormatKey is null)
        {
            return null;
        }

        // Use lower-case for the config value because System.Text.Json doesn't let us create a case-insensitive
        // dictionary. The MediaNamingGuideService converts all parsed guide JSON keys to lower case.
        var lowerKey = configFormatKey.ToLowerInvariant();

        var keys = new List<string> {lowerKey};
        if (keySuffix is not null)
        {
            // Put the more specific key first
            keys.Insert(0, lowerKey + keySuffix);
        }

        foreach (var k in keys)
        {
            if (guideFormats.TryGetValue(k, out var format))
            {
                return format;
            }
        }

        _errors.Add(new InvalidNamingConfig(errorDescription, configFormatKey));
        return null;
    }
}
