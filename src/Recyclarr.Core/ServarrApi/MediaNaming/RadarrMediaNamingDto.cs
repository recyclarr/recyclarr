using System.Text.Json.Serialization;

namespace Recyclarr.ServarrApi.MediaNaming;

public record RadarrMediaNamingDto : MediaNamingDto
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
    public Dictionary<string, object> ExtraJson { get; init; } = new();
}
