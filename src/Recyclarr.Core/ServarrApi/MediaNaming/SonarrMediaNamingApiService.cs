using Flurl.Http;

namespace Recyclarr.ServarrApi.MediaNaming;

public class SonarrMediaNamingApiService(IServarrRequestBuilder service)
    : ISonarrMediaNamingApiService
{
    private IFlurlRequest Request()
    {
        return service.Request("config", "naming");
    }

    public async Task<SonarrMediaNamingDto> GetNaming(CancellationToken ct)
    {
        return await Request().GetJsonAsync<SonarrMediaNamingDto>(cancellationToken: ct);
    }

    public async Task UpdateNaming(SonarrMediaNamingDto dto, CancellationToken ct)
    {
        await Request().PutJsonAsync(dto, cancellationToken: ct);
    }
}
