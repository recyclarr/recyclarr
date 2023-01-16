using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.Radarr.Config;

public class RadarrConfiguration : ServiceConfiguration
{
    public override SupportedServices ServiceType => SupportedServices.Radarr;
}
