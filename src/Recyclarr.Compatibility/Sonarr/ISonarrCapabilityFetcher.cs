namespace Recyclarr.Compatibility.Sonarr;

public interface ISonarrCapabilityFetcher
{
    Task<SonarrCapabilities> GetCapabilities(CancellationToken ct);
}
