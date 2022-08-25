using JetBrains.Annotations;
using TrashLib.Config.Services;

namespace TrashLib.Services.Radarr.Config;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class RadarrConfiguration : ServiceConfiguration
{
    public QualityDefinitionConfig? QualityDefinition { get; init; }
    public ICollection<CustomFormatConfig> CustomFormats { get; init; } = new List<CustomFormatConfig>();
    public bool DeleteOldCustomFormats { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class CustomFormatConfig
{
    public ICollection<string> Names { get; init; } = new List<string>();
    public ICollection<string> TrashIds { get; init; } = new List<string>();
    public ICollection<QualityProfileConfig> QualityProfiles { get; init; } = new List<QualityProfileConfig>();
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class QualityProfileConfig
{
    public string Name { get; init; } = "";
    public int? Score { get; init; }
    public bool ResetUnmatchedScores { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class QualityDefinitionConfig
{
    public string Type { get; init; } = "";
    public decimal PreferredRatio { get; set; } = 1.0m;
}
