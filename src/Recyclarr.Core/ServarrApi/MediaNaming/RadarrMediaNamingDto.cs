using System.Text.Json.Serialization;

namespace Recyclarr.ServarrApi.MediaNaming;

public record RadarrMediaNamingDto : MediaNamingDto
{
    private readonly string? _movieFormat;
    private readonly string? _folderFormat;
    private readonly bool? _renameMovies;

    public string? StandardMovieFormat
    {
        get => _movieFormat;
        init => DtoUtil.SetIfNotNull(ref _movieFormat, value);
    }

    public string? MovieFolderFormat
    {
        get => _folderFormat;
        init => DtoUtil.SetIfNotNull(ref _folderFormat, value);
    }

    public bool? RenameMovies
    {
        get => _renameMovies;
        init => DtoUtil.SetIfNotNull(ref _renameMovies, value);
    }

    [UsedImplicitly]
    [JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = new();
}
