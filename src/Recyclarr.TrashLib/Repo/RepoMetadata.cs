namespace Recyclarr.TrashLib.Repo;

public record RadarrMetadata
{
    public IReadOnlyCollection<string> CustomFormats { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Qualities { get; init; } = Array.Empty<string>();
}

public record SonarrMetadata
{
    public IReadOnlyCollection<string> ReleaseProfiles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Qualities { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> CustomFormats { get; init; } = Array.Empty<string>();
}

public record JsonPaths
{
    public RadarrMetadata Radarr { get; init; } = new();
    public SonarrMetadata Sonarr { get; init; } = new();
}

public record RecyclarrMetadata
{
    public string Templates { get; init; } = "";
}

public record RepoMetadata
{
    public JsonPaths JsonPaths { get; init; } = new();
    public RecyclarrMetadata Recyclarr { get; init; } = new();
}
