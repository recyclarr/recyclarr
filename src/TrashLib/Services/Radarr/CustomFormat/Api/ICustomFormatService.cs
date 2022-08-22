using Newtonsoft.Json.Linq;
using TrashLib.Services.Radarr.CustomFormat.Models;

namespace TrashLib.Services.Radarr.CustomFormat.Api;

public interface ICustomFormatService
{
    Task<List<JObject>> GetCustomFormats();
    Task CreateCustomFormat(ProcessedCustomFormatData cf);
    Task UpdateCustomFormat(ProcessedCustomFormatData cf);
    Task DeleteCustomFormat(int customFormatId);
}
