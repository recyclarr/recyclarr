namespace Recyclarr.ResourceProviders.Domain;

public record QualityProfileGroupResource
{
    public string Name { get; init; } = "";
    public IReadOnlyDictionary<string, string> Profiles { get; init; } =
        new Dictionary<string, string>();
}

public record RadarrQualityProfileGroupResource : QualityProfileGroupResource;

public record SonarrQualityProfileGroupResource : QualityProfileGroupResource;
