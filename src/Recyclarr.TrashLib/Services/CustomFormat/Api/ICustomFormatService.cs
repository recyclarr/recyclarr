using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.CustomFormat.Api;

public interface ICustomFormatService
{
    Task<List<JObject>> GetCustomFormats();
    Task CreateCustomFormat(ProcessedCustomFormatData cf);
    Task UpdateCustomFormat(ProcessedCustomFormatData cf);
    Task DeleteCustomFormat(int customFormatId);
}
