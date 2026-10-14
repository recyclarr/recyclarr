using System.Text.Json.Serialization;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.ResourceProviders.Domain;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record QualitySizeResource
{
    [JsonPropertyName("trash_id")]
    public string TrashId { get; init; } = "";

    public string Type { get; init; } = "";
    public IReadOnlyCollection<QualityItem> Qualities { get; init; } = [];
}

[UsedImplicitly]
public record RadarrQualitySizeResource : QualitySizeResource;

[UsedImplicitly]
public record SonarrQualitySizeResource : QualitySizeResource;
