using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trash.Radarr.CustomFormat.Guide
{
    public interface IRadarrGuideService
    {
        Task<IEnumerable<string>> GetCustomFormatJson();
    }
}
