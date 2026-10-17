namespace Recyclarr.ResourceProviders.Domain;

public record CfGroupCustomFormat
{
    public string TrashId { get; init; } = "";
    public string Name { get; init; } = "";
    public bool Required { get; init; }
    public bool Default { get; init; }
}

public record CfGroupProfiles
{
    public IReadOnlyDictionary<string, string> Include { get; init; } =
        new Dictionary<string, string>();
}

public record CfGroupResource
{
    public string TrashId { get; init; } = "";
    public string Name { get; init; } = "";
    public string TrashDescription { get; init; } = "";
    public string Default { get; init; } = "";
    public IReadOnlyCollection<CfGroupCustomFormat> CustomFormats { get; init; } = [];
    public CfGroupProfiles QualityProfiles { get; init; } = new();
}

public record RadarrCfGroupResource : CfGroupResource;

public record SonarrCfGroupResource : CfGroupResource;
