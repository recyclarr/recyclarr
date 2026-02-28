namespace Recyclarr.Servarr.MediaNaming;

public record SonarrNamingData
{
    public bool? RenameEpisodes { get; init; }
    public string? SeriesFolderFormat { get; init; }
    public string? SeasonFolderFormat { get; init; }
    public string? StandardEpisodeFormat { get; init; }
    public string? DailyEpisodeFormat { get; init; }
    public string? AnimeEpisodeFormat { get; init; }

    public IReadOnlyCollection<string> GetDifferences(SonarrNamingData other)
    {
        List<string> diff = [];
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

    public bool HasValues()
    {
        return RenameEpisodes is not null
            || SeriesFolderFormat is not null
            || SeasonFolderFormat is not null
            || StandardEpisodeFormat is not null
            || DailyEpisodeFormat is not null
            || AnimeEpisodeFormat is not null;
    }
}
