using System.Text.Json.Serialization;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.ResourceProviders.Domain;

public record QualitySizeResource
{
    [JsonPropertyName("trash_id")]
    public string TrashId { get; init; } = "";

    public string Type { get; init; } = "";
    public IReadOnlyCollection<QualityItem> Qualities { get; init; } = [];
}

public record RadarrQualitySizeResource : QualitySizeResource;

public record SonarrQualitySizeResource : QualitySizeResource;
