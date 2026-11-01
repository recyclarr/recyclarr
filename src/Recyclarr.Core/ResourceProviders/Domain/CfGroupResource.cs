namespace Recyclarr.ResourceProviders.Domain;

public record CfGroupCustomFormat
{
    public string TrashId { get; init; } = "";
    public string Name { get; init; } = "";
    public bool Required { get; init; }
    public bool Default { get; init; }

    /// <summary>A member synced only when the user selects it.</summary>
    public bool IsOptional => !Required && !Default;
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

    /// <summary>
    /// A default group syncs automatically for every configured profile it includes. The guide
    /// stores the flag as the string "true".
    /// </summary>
    public bool IsDefault => Default.Equals("true", StringComparison.OrdinalIgnoreCase);

    public IReadOnlySet<string> IncludedProfileTrashIds =>
        QualityProfiles.Include.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
}

public record RadarrCfGroupResource : CfGroupResource;

public record SonarrCfGroupResource : CfGroupResource;
