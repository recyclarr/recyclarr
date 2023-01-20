using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.CustomFormat.Api;

public interface ICustomFormatService
{
    Task<List<JObject>> GetCustomFormats(IServiceConfiguration config);
    Task CreateCustomFormat(IServiceConfiguration config, ProcessedCustomFormatData cf);
    Task UpdateCustomFormat(IServiceConfiguration config, ProcessedCustomFormatData cf);
    Task DeleteCustomFormat(IServiceConfiguration config, int customFormatId);
}
