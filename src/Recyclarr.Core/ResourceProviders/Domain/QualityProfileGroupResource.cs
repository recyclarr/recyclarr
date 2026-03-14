namespace Recyclarr.ResourceProviders.Domain;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record QualityProfileGroupResource
{
    public string Name { get; init; } = "";
    public IReadOnlyDictionary<string, string> Profiles { get; init; } =
        new Dictionary<string, string>();
}

[UsedImplicitly]
public record RadarrQualityProfileGroupResource : QualityProfileGroupResource;

[UsedImplicitly]
public record SonarrQualityProfileGroupResource : QualityProfileGroupResource;
