using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Services;

public abstract class ServiceConfiguration : IServiceConfiguration
{
    // Name is set dynamically by ConfigurationLoader
    [YamlIgnore]
    public string? Name { get; set; }

    [YamlIgnore]
    public int LineNumber { get; set; }

    public string BaseUrl { get; set; } = "";
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
