using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.ResourceProviders.Domain;

public record QualitySizeResource
{
    public string Type { get; init; } = "";
    public IReadOnlyCollection<QualityItem> Qualities { get; init; } = [];
}

public record RadarrQualitySizeResource : QualitySizeResource;

public record SonarrQualitySizeResource : QualitySizeResource;
