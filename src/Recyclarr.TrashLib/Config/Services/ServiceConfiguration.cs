using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Services;

public abstract class ServiceConfiguration : IServiceConfiguration
{
    [YamlIgnore]
    public abstract SupportedServices ServiceType { get; }

    // Name is set dynamically by ConfigurationLoader
    [YamlIgnore]
    public string? InstanceName { get; set; }

    [YamlIgnore]
    public int LineNumber { get; set; }

    public Uri BaseUrl { get; set; } = new("about:empty");
    public string ApiKey { get; set; } = "";

    public ICollection<CustomFormatConfig> CustomFormats { get; init; } =
        new List<CustomFormatConfig>();

    public bool DeleteOldCustomFormats { get; init; }

    public QualityDefinitionConfig? QualityDefinition { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class CustomFormatConfig
{
    public ICollection<string> TrashIds { get; init; } = new List<string>();

    public ICollection<QualityProfileScoreConfig> QualityProfiles { get; init; } =
        new List<QualityProfileScoreConfig>();
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class QualityProfileScoreConfig
{
    public string Name { get; init; } = "";
    public int? Score { get; init; }
    public bool ResetUnmatchedScores { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class QualityDefinitionConfig
{
    public string Type { get; init; } = "";
    public decimal? PreferredRatio { get; set; }
}
