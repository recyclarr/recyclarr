namespace Recyclarr.Servarr.MediaNaming;

public interface ISonarrNamingService
{
    Task<SonarrNamingData> GetNaming(CancellationToken ct);
    Task UpdateNaming(SonarrNamingData data, CancellationToken ct);
}
