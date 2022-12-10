using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace TrashLib.Config.Services;

public abstract class ServiceConfiguration : IServiceConfiguration
{
    // Name is set dynamically by ConfigurationLoader
    [YamlIgnore]
    public string? Name { get; set; }

    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";

    public ICollection<CustomFormatConfig> CustomFormats { get; init; } =
        new List<CustomFormatConfig>();

    public bool DeleteOldCustomFormats { get; init; }
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
