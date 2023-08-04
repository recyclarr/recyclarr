using JetBrains.Annotations;

namespace Recyclarr.TrashLib.Config.Services;

public abstract record ServiceConfiguration : IServiceConfiguration
{
    public abstract SupportedServices ServiceType { get; }
    public required string InstanceName { get; init; }

    public Uri BaseUrl { get; set; } = new("about:empty");
    public string ApiKey { get; init; } = "";

    public ICollection<CustomFormatConfig> CustomFormats { get; init; } =
        new List<CustomFormatConfig>();

    public bool DeleteOldCustomFormats { get; [UsedImplicitly] init; }
    public bool ReplaceExistingCustomFormats { get; init; }

    public QualityDefinitionConfig? QualityDefinition { get; init; }

    public IReadOnlyCollection<QualityProfileConfig> QualityProfiles { get; init; } =
        Array.Empty<QualityProfileConfig>();
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record CustomFormatConfig
{
    public ICollection<string> TrashIds { get; init; } = new List<string>();

    public ICollection<QualityProfileScoreConfig> QualityProfiles { get; init; } =
        new List<QualityProfileScoreConfig>();
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualityProfileScoreConfig
{
    public string Name { get; init; } = "";
    public int? Score { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualityDefinitionConfig
{
    public string Type { get; init; } = "";
    public decimal? PreferredRatio { get; set; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualityProfileQualityConfig
{
    public string Name { get; init; } = "";
    public bool Enabled { get; init; }
    public IReadOnlyCollection<string> Qualities { get; init; } = Array.Empty<string>();
}

public enum QualitySortAlgorithm
{
    Top,
    Bottom
}

public record QualityProfileConfig
{
    public string Name { get; init; } = "";
    public bool? UpgradeAllowed { get; init; }
    public string? UpgradeUntilQuality { get; init; }
    public int? UpgradeUntilScore { get; init; }
    public int? MinFormatScore { get; init; }
    public bool ResetUnmatchedScores { get; init; }
    public QualitySortAlgorithm QualitySort { get; init; }
    public IReadOnlyCollection<QualityProfileQualityConfig> Qualities { get; init; } =
        Array.Empty<QualityProfileQualityConfig>();
}
