namespace Recyclarr.TrashLib.Config.Services;

public record RadarrConfiguration : ServiceConfiguration
{
    public override SupportedServices ServiceType => SupportedServices.Radarr;
}
