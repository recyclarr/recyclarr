using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Recyclarr.SyncState;

namespace Recyclarr.ResourceProviders.Domain;

public record QualityProfileQualityItem
{
    public string Name { get; init; } = "";
    public bool Allowed { get; init; }
    public IReadOnlyCollection<string> Items { get; init; } = [];
}

[SuppressMessage(
    "Design",
    "CA1056:URI properties should not be strings",
    Justification = "JSON DTO with optional URL field"
)]
public record QualityProfileResource : IGuideResource
{
    [JsonPropertyName("trash_id")]
    public string TrashId { get; init; } = "";

    public string Name { get; init; } = "";

    [JsonIgnore]
    public MappingKey MappingKey => new(TrashId, Name);

    [JsonPropertyName("trash_url")]
    public string TrashUrl { get; init; } = "";

    [JsonPropertyName("trash_score_set")]
    public string TrashScoreSet { get; init; } = "";

    public int Group { get; init; }
    public bool UpgradeAllowed { get; init; }
    public string Cutoff { get; init; } = "";
    public int MinFormatScore { get; init; }
    public int CutoffFormatScore { get; init; }
    public int MinUpgradeFormatScore { get; init; }
    public IReadOnlyCollection<QualityProfileQualityItem> Items { get; init; } = [];
    public IReadOnlyDictionary<string, string> FormatItems { get; init; } =
        new Dictionary<string, string>();

    // Single entry mapping a readable label to the trash_id of the quality size resource whose
    // sizes this profile uses on services that store sizes per profile. Same shape as
    // FormatItems; only the trash_id value is used for matching.
    [JsonPropertyName("trash_quality_definition")]
    public IReadOnlyDictionary<string, string> TrashQualityDefinition { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>
/// A Radarr guide profile. Only Radarr profiles have a language (ADR-023); Sonarr guide JSON may
/// still contain one, and it is ignored.
/// </summary>
public record RadarrQualityProfileResource : QualityProfileResource
{
    public string Language { get; init; } = "";
}

public record SonarrQualityProfileResource : QualityProfileResource;
