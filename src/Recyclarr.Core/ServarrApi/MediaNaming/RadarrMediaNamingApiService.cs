using Flurl.Http;

namespace Recyclarr.ServarrApi.MediaNaming;

public class RadarrMediaNamingApiService(IServarrRequestBuilder service)
    : IRadarrMediaNamingApiService
{
    private IFlurlRequest Request()
    {
        return service.Request("config", "naming");
    }

    public async Task<RadarrMediaNamingDto> GetNaming(CancellationToken ct)
    {
        return await Request().GetJsonAsync<RadarrMediaNamingDto>(cancellationToken: ct);
    }

    public async Task UpdateNaming(RadarrMediaNamingDto dto, CancellationToken ct)
    {
        await Request().PutJsonAsync(dto, cancellationToken: ct);
    }
}
