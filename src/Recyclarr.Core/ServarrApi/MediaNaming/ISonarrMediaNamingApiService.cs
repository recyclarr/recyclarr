namespace Recyclarr.ServarrApi.MediaNaming;

public interface ISonarrMediaNamingApiService
{
    Task<ServiceSonarrNamingData> GetNaming(CancellationToken ct);
    Task UpdateNaming(ServiceSonarrNamingData dto, CancellationToken ct);
}
