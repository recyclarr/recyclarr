using System.Text.Json.Serialization;

namespace Recyclarr.ServarrApi.MediaNaming;

public record SonarrMediaNamingDto
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

    public IReadOnlyCollection<string> GetDifferences(SonarrMediaNamingDto other)
    {
        var diff = new List<string>();

        if (RenameEpisodes != other.RenameEpisodes)
        {
            diff.Add(nameof(RenameEpisodes));
        }

        if (SeasonFolderFormat != other.SeasonFolderFormat)
        {
            diff.Add(nameof(SeasonFolderFormat));
        }

        if (SeriesFolderFormat != other.SeriesFolderFormat)
        {
            diff.Add(nameof(SeriesFolderFormat));
        }

        if (StandardEpisodeFormat != other.StandardEpisodeFormat)
        {
            diff.Add(nameof(StandardEpisodeFormat));
        }

        if (DailyEpisodeFormat != other.DailyEpisodeFormat)
        {
            diff.Add(nameof(DailyEpisodeFormat));
        }

        if (AnimeEpisodeFormat != other.AnimeEpisodeFormat)
        {
            diff.Add(nameof(AnimeEpisodeFormat));
        }

        return diff;
    }
}
