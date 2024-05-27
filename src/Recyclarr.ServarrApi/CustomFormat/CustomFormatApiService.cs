using Flurl.Http;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ServarrApi.CustomFormat;

public class CustomFormatApiService(IServarrRequestBuilder service) : ICustomFormatApiService
{
    private IFlurlRequest Request(params object[] path)
    {
        return service.Request(["customformat", ..path]);
    }

    public async Task<IList<CustomFormatData>> GetCustomFormats()
    {
        return await Request()
            .GetJsonAsync<IList<CustomFormatData>>();
    }

    public async Task<CustomFormatData?> CreateCustomFormat(CustomFormatData cf)
    {
        return await Request()
            .PostJsonAsync(cf)
            .ReceiveJson<CustomFormatData>();
    }

    public async Task UpdateCustomFormat(CustomFormatData cf)
    {
        await Request(cf.Id)
            .PutJsonAsync(cf);
    }

    public async Task DeleteCustomFormat(int customFormatId, CancellationToken cancellationToken = default)
    {
        await Request(customFormatId)
            .DeleteAsync(cancellationToken: cancellationToken);
    }
}
