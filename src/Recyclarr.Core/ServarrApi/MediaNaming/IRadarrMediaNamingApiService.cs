namespace Recyclarr.ServarrApi.MediaNaming;

public interface IRadarrMediaNamingApiService
{
    Task<ServiceRadarrNamingData> GetNaming(CancellationToken ct);
    Task UpdateNaming(ServiceRadarrNamingData dto, CancellationToken ct);
}
