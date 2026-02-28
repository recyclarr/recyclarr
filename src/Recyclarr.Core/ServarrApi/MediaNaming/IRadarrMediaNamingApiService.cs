namespace Recyclarr.ServarrApi.MediaNaming;

public interface IRadarrMediaNamingApiService
{
    Task<RadarrMediaNamingDto> GetNaming(CancellationToken ct);
    Task UpdateNaming(RadarrMediaNamingDto dto, CancellationToken ct);
}
