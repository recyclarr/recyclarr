using System.Collections.Generic;
using System.Threading.Tasks;
using TrashLib.Radarr.CustomFormat.Api.Models;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Api
{
    public interface ICustomFormatService
    {
        Task<List<CustomFormatData>> GetCustomFormats();
        Task<int> CreateCustomFormat(ProcessedCustomFormatData cf);
        Task UpdateCustomFormat(int formatId, ProcessedCustomFormatData cf);
        Task DeleteCustomFormat(int formatId);
    }
}
