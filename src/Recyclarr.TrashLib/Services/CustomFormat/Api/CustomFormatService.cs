using Flurl.Http;
using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.CustomFormat.Api;

internal class CustomFormatService : ICustomFormatService
{
    private readonly IServiceRequestBuilder _service;

    public CustomFormatService(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task<List<JObject>> GetCustomFormats(IServiceConfiguration config)
    {
        return await _service.Request(config, "customformat")
            .GetJsonAsync<List<JObject>>();
    }

    public async Task CreateCustomFormat(IServiceConfiguration config, ProcessedCustomFormatData cf)
    {
        var response = await _service.Request(config, "customformat")
            .PostJsonAsync(cf.Json)
            .ReceiveJson<JObject>();

        if (response != null)
        {
            cf.FormatId = response.Value<int>("id");
        }
    }

    public async Task UpdateCustomFormat(IServiceConfiguration config, ProcessedCustomFormatData cf)
    {
        await _service.Request(config, "customformat", cf.FormatId)
            .PutJsonAsync(cf.Json)
            .ReceiveJson<JObject>();
    }

    public async Task DeleteCustomFormat(IServiceConfiguration config, int customFormatId)
    {
        await _service.Request(config, "customformat", customFormatId)
            .DeleteAsync();
    }
}
