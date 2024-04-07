using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ServarrApi.CustomFormat;

public class CustomFormatApiService(IServarrRequestBuilder service) : ICustomFormatApiService
{
    public async Task<IList<CustomFormatData>> GetCustomFormats(IServiceConfiguration config)
    {
        return await service.Request(config, "customformat")
            .GetJsonAsync<IList<CustomFormatData>>();
    }

    public async Task<CustomFormatData?> CreateCustomFormat(IServiceConfiguration config, CustomFormatData cf)
    {
        return await service.Request(config, "customformat")
            .PostJsonAsync(cf)
            .ReceiveJson<CustomFormatData>();
    }

    public async Task UpdateCustomFormat(IServiceConfiguration config, CustomFormatData cf)
    {
        await service.Request(config, "customformat", cf.Id)
            .PutJsonAsync(cf);
    }

    public async Task DeleteCustomFormat(
        IServiceConfiguration config,
        int customFormatId,
        CancellationToken cancellationToken = default)
    {
        await service.Request(config, "customformat", customFormatId)
            .DeleteAsync(cancellationToken: cancellationToken);
    }
}
