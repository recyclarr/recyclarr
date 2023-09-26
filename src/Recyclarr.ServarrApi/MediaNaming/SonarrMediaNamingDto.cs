using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Recyclarr.ServarrApi.MediaNaming;

public record SonarrMediaNamingDto : MediaNamingDto
{
    private readonly string? _seriesFolderFormat;
    private readonly string? _seasonFolderFormat;
    private string? _standardEpisodeFormat;
    private string? _dailyEpisodeFormat;
    private string? _animeEpisodeFormat;
    private readonly bool? _renameEpisodes;

    public string? SeriesFolderFormat
    {
        get => _seriesFolderFormat;
        init => DtoUtil.SetIfNotNull(ref _seriesFolderFormat, value);
    }

    public string? SeasonFolderFormat
    {
        get => _seasonFolderFormat;
        init => DtoUtil.SetIfNotNull(ref _seasonFolderFormat, value);
    }

    public string? StandardEpisodeFormat
    {
        get => _standardEpisodeFormat;
        set => DtoUtil.SetIfNotNull(ref _standardEpisodeFormat, value);
    }

    public string? DailyEpisodeFormat
    {
        get => _dailyEpisodeFormat;
        set => DtoUtil.SetIfNotNull(ref _dailyEpisodeFormat, value);
    }

    public string? AnimeEpisodeFormat
    {
        get => _animeEpisodeFormat;
        set => DtoUtil.SetIfNotNull(ref _animeEpisodeFormat, value);
    }

    public bool? RenameEpisodes
    {
        get => _renameEpisodes;
        init => DtoUtil.SetIfNotNull(ref _renameEpisodes, value);
    }

    [UsedImplicitly, JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = new();
}
