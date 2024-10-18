namespace Recyclarr.ServarrApi.MediaNaming;

public static class MediaNamingDtoExtensions
{
    public static IReadOnlyCollection<string> GetDifferences(this RadarrMediaNamingDto left, MediaNamingDto other)
    {
        var diff = new List<string>();

        if (other is not RadarrMediaNamingDto right)
        {
            throw new ArgumentException("'other' is of the wrong type");
        }

        if (left.RenameMovies != right.RenameMovies)
        {
            diff.Add(nameof(left.RenameMovies));
        }

        if (left.MovieFolderFormat != right.MovieFolderFormat)
        {
            diff.Add(nameof(left.MovieFolderFormat));
        }

        if (left.StandardMovieFormat != right.StandardMovieFormat)
        {
            diff.Add(nameof(left.StandardMovieFormat));
        }

        return diff;
    }

    public static IReadOnlyCollection<string> GetDifferences(this SonarrMediaNamingDto left, MediaNamingDto other)
    {
        var diff = new List<string>();

        if (other is not SonarrMediaNamingDto right)
        {
            throw new ArgumentException("'other' is of the wrong type");
        }

        if (left.RenameEpisodes != right.RenameEpisodes)
        {
            diff.Add(nameof(left.RenameEpisodes));
        }

        if (left.SeasonFolderFormat != right.SeasonFolderFormat)
        {
            diff.Add(nameof(left.SeasonFolderFormat));
        }

        if (left.SeriesFolderFormat != right.SeriesFolderFormat)
        {
            diff.Add(nameof(left.SeriesFolderFormat));
        }

        if (left.StandardEpisodeFormat != right.StandardEpisodeFormat)
        {
            diff.Add(nameof(left.StandardEpisodeFormat));
        }

        if (left.DailyEpisodeFormat != right.DailyEpisodeFormat)
        {
            diff.Add(nameof(left.DailyEpisodeFormat));
        }

        if (left.AnimeEpisodeFormat != right.AnimeEpisodeFormat)
        {
            diff.Add(nameof(left.AnimeEpisodeFormat));
        }

        return diff;
    }
}
