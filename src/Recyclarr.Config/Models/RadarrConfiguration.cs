using Recyclarr.Common;

namespace Recyclarr.Config.Models;

public record RadarrConfiguration : ServiceConfiguration
{
    public override SupportedServices ServiceType => SupportedServices.Radarr;
}
