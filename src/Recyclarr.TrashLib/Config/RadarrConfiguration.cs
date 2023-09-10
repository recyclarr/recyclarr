namespace Recyclarr.TrashLib.Config;

public record RadarrConfiguration : ServiceConfiguration
{
    public override SupportedServices ServiceType => SupportedServices.Radarr;
}
