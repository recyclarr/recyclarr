using Flurl.Http;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ServarrApi.CustomFormat;

public class CustomFormatApiService(IServarrRequestBuilder service) : ICustomFormatApiService
{
    public async Task<IList<CustomFormatData>> GetCustomFormats()
    {
        return await service.Request("customformat")
            .GetJsonAsync<IList<CustomFormatData>>();
    }

    public async Task<CustomFormatData?> CreateCustomFormat(CustomFormatData cf)
    {
        return await service.Request("customformat")
            .PostJsonAsync(cf)
            .ReceiveJson<CustomFormatData>();
    }

    public async Task UpdateCustomFormat(CustomFormatData cf)
    {
        await service.Request("customformat", cf.Id)
            .PutJsonAsync(cf);
    }

    public async Task DeleteCustomFormat(int customFormatId, CancellationToken cancellationToken = default)
    {
        await service.Request("customformat", customFormatId)
            .DeleteAsync(cancellationToken: cancellationToken);
    }
}
