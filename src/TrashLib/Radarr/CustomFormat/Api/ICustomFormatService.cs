using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Api
{
    public interface ICustomFormatService
    {
        Task<List<JObject>> GetCustomFormats();
        Task CreateCustomFormat(ProcessedCustomFormatData cf);
        Task UpdateCustomFormat(ProcessedCustomFormatData cf);
        Task DeleteCustomFormat(int customFormatId);
    }
}
