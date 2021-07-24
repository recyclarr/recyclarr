using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrashLib.Radarr.CustomFormat.Guide
{
    public interface IRadarrGuideService
    {
        Task<IEnumerable<string>> GetCustomFormatJsonAsync();
    }
}
