using Flurl.Http;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ServarrApi.CustomFormat;

public class CustomFormatApiService(IServarrRequestBuilder service) : ICustomFormatApiService
{
    private IFlurlRequest Request(params object[] path)
    {
        return service.Request(["customformat", ..path]);
    }

    public async Task<IList<CustomFormatData>> GetCustomFormats(CancellationToken ct)
    {
        return await Request()
            .GetJsonAsync<IList<CustomFormatData>>(cancellationToken: ct);
    }

    public async Task<CustomFormatData?> CreateCustomFormat(CustomFormatData cf, CancellationToken ct)
    {
        return await Request()
            .PostJsonAsync(cf, cancellationToken: ct)
            .ReceiveJson<CustomFormatData>();
    }

    public async Task UpdateCustomFormat(CustomFormatData cf, CancellationToken ct)
    {
        await Request(cf.Id)
            .PutJsonAsync(cf, cancellationToken: ct);
    }

    public async Task DeleteCustomFormat(int customFormatId, CancellationToken ct)
    {
        await Request(customFormatId)
            .DeleteAsync(cancellationToken: ct);
    }
}
