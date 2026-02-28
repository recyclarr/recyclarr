namespace Recyclarr.Servarr.MediaNaming;

public interface IRadarrNamingService
{
    Task<RadarrNamingData> GetNaming(CancellationToken ct);
    Task UpdateNaming(RadarrNamingData data, CancellationToken ct);
}
