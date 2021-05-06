using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Trash.Radarr.CustomFormat.Models;

namespace Trash.Radarr.CustomFormat.Api
{
    public interface ICustomFormatService
    {
        Task<List<JObject>> GetCustomFormats();
        Task CreateCustomFormat(ProcessedCustomFormatData cf);
        Task UpdateCustomFormat(ProcessedCustomFormatData cf);
        Task DeleteCustomFormat(int customFormatId);
    }
}
