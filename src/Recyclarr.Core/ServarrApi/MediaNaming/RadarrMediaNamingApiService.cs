using Flurl.Http;

namespace Recyclarr.ServarrApi.MediaNaming;

public class RadarrMediaNamingApiService(IServarrRequestBuilder service)
    : IRadarrMediaNamingApiService
{
    private IFlurlRequest Request()
    {
        return service.Request("config", "naming");
    }

    public async Task<ServiceRadarrNamingData> GetNaming(CancellationToken ct)
    {
        return await Request().GetJsonAsync<ServiceRadarrNamingData>(cancellationToken: ct);
    }

    public async Task UpdateNaming(ServiceRadarrNamingData dto, CancellationToken ct)
    {
        await Request().PutJsonAsync(dto, cancellationToken: ct);
    }
}
