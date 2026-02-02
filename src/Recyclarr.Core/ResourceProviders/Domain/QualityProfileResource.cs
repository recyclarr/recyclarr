using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Recyclarr.SyncState;

namespace Recyclarr.ResourceProviders.Domain;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record QualityProfileQualityItem
{
    public string Name { get; init; } = "";
    public bool Allowed { get; init; }
    public IReadOnlyCollection<string> Items { get; init; } = [];
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
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
    public string Language { get; init; } = "";
    public IReadOnlyCollection<QualityProfileQualityItem> Items { get; init; } = [];
    public IReadOnlyDictionary<string, string> FormatItems { get; init; } =
        new Dictionary<string, string>();
}

[UsedImplicitly]
public record RadarrQualityProfileResource : QualityProfileResource;

[UsedImplicitly]
public record SonarrQualityProfileResource : QualityProfileResource;
