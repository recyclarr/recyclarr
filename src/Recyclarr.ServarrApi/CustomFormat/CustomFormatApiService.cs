using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Http;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ServarrApi.CustomFormat;

public class CustomFormatApiService : ICustomFormatApiService
{
    private readonly IServiceRequestBuilder _service;

    public CustomFormatApiService(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task<IList<CustomFormatData>> GetCustomFormats(IServiceConfiguration config)
    {
        return await _service.Request(config, "customformat")
            .GetJsonAsync<IList<CustomFormatData>>();
    }

    public async Task<CustomFormatData?> CreateCustomFormat(IServiceConfiguration config, CustomFormatData cf)
    {
        return await _service.Request(config, "customformat")
            .PostJsonAsync(cf)
            .ReceiveJson<CustomFormatData>();
    }

    public async Task UpdateCustomFormat(IServiceConfiguration config, CustomFormatData cf)
    {
        await _service.Request(config, "customformat", cf.Id)
            .PutJsonAsync(cf);
    }

    public async Task DeleteCustomFormat(
        IServiceConfiguration config,
        int customFormatId,
        CancellationToken cancellationToken = default)
    {
        await _service.Request(config, "customformat", customFormatId)
            .DeleteAsync(cancellationToken: cancellationToken);
    }
}
