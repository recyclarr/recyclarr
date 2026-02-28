using System.Text.Json.Serialization;

namespace Recyclarr.ServarrApi.MediaNaming;

public record RadarrMediaNamingDto
{
    public string? StandardMovieFormat
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public string? MovieFolderFormat
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public bool? RenameMovies
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    [UsedImplicitly]
    [JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = [];

    public IReadOnlyCollection<string> GetDifferences(RadarrMediaNamingDto other)
    {
        var diff = new List<string>();

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
}
