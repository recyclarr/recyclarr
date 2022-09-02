using JetBrains.Annotations;
using TrashLib.Config.Services;

namespace TrashLib.Services.Radarr.Config;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class RadarrConfiguration : ServiceConfiguration
{
    public QualityDefinitionConfig? QualityDefinition { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class QualityDefinitionConfig
{
    public string Type { get; init; } = "";
    public decimal PreferredRatio { get; set; } = 1.0m;
}
