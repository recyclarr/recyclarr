using System.Text.Json.Serialization;

namespace Recyclarr.ServarrApi.MediaNaming;

public record SonarrMediaNamingDto : MediaNamingDto
{
    public string? SeriesFolderFormat
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public string? SeasonFolderFormat
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public string? StandardEpisodeFormat
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public string? DailyEpisodeFormat
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public string? AnimeEpisodeFormat
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public bool? RenameEpisodes
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    [UsedImplicitly]
    [JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = [];
}
