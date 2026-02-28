namespace Recyclarr.Servarr.MediaNaming;

public record RadarrNamingData
{
    public bool? RenameMovies { get; init; }
    public string? StandardMovieFormat { get; init; }
    public string? MovieFolderFormat { get; init; }

    public IReadOnlyCollection<string> GetDifferences(RadarrNamingData other)
    {
        List<string> diff = [];
        if (RenameMovies != other.RenameMovies)
        {
            diff.Add(nameof(RenameMovies));
        }

        if (MovieFolderFormat != other.MovieFolderFormat)
        {
            diff.Add(nameof(MovieFolderFormat));
        }

        if (StandardMovieFormat != other.StandardMovieFormat)
        {
            diff.Add(nameof(StandardMovieFormat));
        }

        return diff;
    }

    public bool HasValues()
    {
        return RenameMovies is not null
            || StandardMovieFormat is not null
            || MovieFolderFormat is not null;
    }
}
