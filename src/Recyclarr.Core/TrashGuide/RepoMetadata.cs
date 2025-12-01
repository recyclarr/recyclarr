using System.Text.Json.Serialization;

namespace Recyclarr.TrashGuide;

public record RadarrMetadata
{
    public IReadOnlyCollection<string> CustomFormats { get; init; } = [];
    public IReadOnlyCollection<string> Qualities { get; init; } = [];
    public IReadOnlyCollection<string> Naming { get; init; } = [];

    [JsonPropertyName("custom_format_groups")]
    public IReadOnlyCollection<string> CfGroups { get; init; } = [];

    public IReadOnlyCollection<string> QualityProfiles { get; init; } = [];
}

public record SonarrMetadata
{
    public IReadOnlyCollection<string> Qualities { get; init; } = [];
    public IReadOnlyCollection<string> CustomFormats { get; init; } = [];
    public IReadOnlyCollection<string> Naming { get; init; } = [];

    [JsonPropertyName("custom_format_groups")]
    public IReadOnlyCollection<string> CfGroups { get; init; } = [];

    public IReadOnlyCollection<string> QualityProfiles { get; init; } = [];
}

public record JsonPaths
{
    public RadarrMetadata Radarr { get; init; } = new();
    public SonarrMetadata Sonarr { get; init; } = new();
}

public record RepoMetadata
{
    public JsonPaths JsonPaths { get; init; } = new();
}
