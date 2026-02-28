namespace Recyclarr.ServarrApi.MediaNaming;

public interface ISonarrMediaNamingApiService
{
    Task<SonarrMediaNamingDto> GetNaming(CancellationToken ct);
    Task UpdateNaming(SonarrMediaNamingDto dto, CancellationToken ct);
}
