using JetBrains.Annotations;

namespace Recyclarr.TrashLib.Config.Services;

public abstract record ServiceConfiguration : IServiceConfiguration
{
    public abstract SupportedServices ServiceType { get; }
    public string? InstanceName { get; set; }

    public Uri BaseUrl { get; set; } = new("about:empty");
    public string ApiKey { get; init; } = "";

    public ICollection<CustomFormatConfig> CustomFormats { get; init; } =
        new List<CustomFormatConfig>();

    public bool DeleteOldCustomFormats { get; [UsedImplicitly] init; }
    public bool ReplaceExistingCustomFormats { get; init; }

    public QualityDefinitionConfig? QualityDefinition { get; init; }

    // todo: Remove the setter later once ResetUnmatchedScores is removed from custom format quality profiles property
    public IReadOnlyCollection<QualityProfileConfig> QualityProfiles { get; set; } =
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
    public bool? ResetUnmatchedScores { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualityDefinitionConfig
{
    public string Type { get; init; } = "";
    public decimal? PreferredRatio { get; set; }
}

public record QualityProfileConfig
{
    // todo: Remove the setter later once reset_unmatched_scores is not in the cf.quality_profiles property anymore
    public bool? ResetUnmatchedScores { get; set; }
    public string Name { get; init; } = "";
}
