using JetBrains.Annotations;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.Radarr.Config;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class RadarrConfiguration : ServiceConfiguration
{
    public override string ServiceName { get; } = SupportedServices.Radarr.ToString();
}
