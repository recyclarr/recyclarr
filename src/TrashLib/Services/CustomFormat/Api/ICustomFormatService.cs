using Newtonsoft.Json.Linq;
using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.CustomFormat.Api;

public interface ICustomFormatService
{
    Task<List<JObject>> GetCustomFormats();
    Task CreateCustomFormat(ProcessedCustomFormatData cf);
    Task UpdateCustomFormat(ProcessedCustomFormatData cf);
    Task DeleteCustomFormat(int customFormatId);
}
