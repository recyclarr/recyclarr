using Flurl.Http;
using Newtonsoft.Json.Linq;
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

    public async Task<List<JObject>> GetCustomFormats()
    {
        return await _service.Request("customformat")
            .GetJsonAsync<List<JObject>>();
    }

    public async Task CreateCustomFormat(ProcessedCustomFormatData cf)
    {
        var response = await _service.Request("customformat")
            .PostJsonAsync(cf.Json)
            .ReceiveJson<JObject>();

        if (response != null)
        {
            cf.FormatId = response.Value<int>("id");
        }
    }

    public async Task UpdateCustomFormat(ProcessedCustomFormatData cf)
    {
        await _service.Request("customformat", cf.FormatId)
            .PutJsonAsync(cf.Json)
            .ReceiveJson<JObject>();
    }

    public async Task DeleteCustomFormat(int customFormatId)
    {
        await _service.Request("customformat", customFormatId)
            .DeleteAsync();
    }
}
