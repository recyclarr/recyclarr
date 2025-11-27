using Flurl.Http;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.ServarrApi.CustomFormat;

public class CustomFormatApiService(IServarrRequestBuilder service) : ICustomFormatApiService
{
    private IFlurlRequest Request(params object[] path)
    {
        return service.Request(["customformat", .. path]);
    }

    public async Task<IList<CustomFormatResource>> GetCustomFormats(CancellationToken ct)
    {
        return await Request().GetJsonAsync<IList<CustomFormatResource>>(cancellationToken: ct);
    }

    public async Task<CustomFormatResource?> CreateCustomFormat(
        CustomFormatResource cf,
        CancellationToken ct
    )
    {
        return await Request()
            .PostJsonAsync(cf, cancellationToken: ct)
            .ReceiveJson<CustomFormatResource>();
    }

    public async Task UpdateCustomFormat(CustomFormatResource cf, CancellationToken ct)
    {
        await Request(cf.Id).PutJsonAsync(cf, cancellationToken: ct);
    }

    public async Task DeleteCustomFormat(int customFormatId, CancellationToken ct)
    {
        await Request(customFormatId).DeleteAsync(cancellationToken: ct);
    }
}
