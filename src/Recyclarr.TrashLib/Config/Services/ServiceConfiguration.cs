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

public record QualityProfileConfig
{
    public bool? ResetUnmatchedScores { get; init; }
    public string Name { get; init; } = "";
}
