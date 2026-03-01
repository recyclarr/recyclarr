using Flurl.Http;

namespace Recyclarr.ServarrApi.MediaNaming;

public class SonarrMediaNamingApiService(IServarrRequestBuilder service)
    : ISonarrMediaNamingApiService
{
    private IFlurlRequest Request()
    {
        return service.Request("config", "naming");
    }

    public async Task<ServiceSonarrNamingData> GetNaming(CancellationToken ct)
    {
        return await Request().GetJsonAsync<ServiceSonarrNamingData>(cancellationToken: ct);
    }

    public async Task UpdateNaming(ServiceSonarrNamingData dto, CancellationToken ct)
    {
        await Request().PutJsonAsync(dto, cancellationToken: ct);
    }
}
