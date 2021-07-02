using System.Collections.Generic;
using System.Threading.Tasks;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;

namespace Recyclarr.Code.Radarr
{
    public interface IGuideProcessor
    {
        IReadOnlyCollection<ProcessedCustomFormatData> CustomFormats { get; }
        bool IsLoaded { get; }
        Task ForceBuildGuideData(RadarrConfig config);
        Task<bool> BuildGuideData(RadarrConfig config);
        Task SaveToRadarr(RadarrConfig config);
    }
}
