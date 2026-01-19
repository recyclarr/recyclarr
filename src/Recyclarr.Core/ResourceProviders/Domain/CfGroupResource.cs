namespace Recyclarr.ResourceProviders.Domain;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record CfGroupCustomFormat
{
    public string TrashId { get; init; } = "";
    public string Name { get; init; } = "";
    public bool Required { get; init; }
    public bool Default { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record CfGroupProfiles
{
    public IReadOnlyDictionary<string, string> Include { get; init; } =
        new Dictionary<string, string>();
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record CfGroupResource
{
    public string TrashId { get; init; } = "";
    public string Name { get; init; } = "";
    public string TrashDescription { get; init; } = "";
    public string Default { get; init; } = "";
    public IReadOnlyCollection<CfGroupCustomFormat> CustomFormats { get; init; } = [];
    public CfGroupProfiles QualityProfiles { get; init; } = new();
}

[UsedImplicitly]
public record RadarrCfGroupResource : CfGroupResource;

[UsedImplicitly]
public record SonarrCfGroupResource : CfGroupResource;
