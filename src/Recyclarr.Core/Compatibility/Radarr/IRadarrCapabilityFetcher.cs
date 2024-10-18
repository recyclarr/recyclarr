namespace Recyclarr.Compatibility.Radarr;

public interface IRadarrCapabilityFetcher
{
    Task<RadarrCapabilities> GetCapabilities(CancellationToken ct);
}
