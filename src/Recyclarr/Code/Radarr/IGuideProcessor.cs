using System.Collections.Generic;
using System.Threading.Tasks;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace Recyclarr.Code.Radarr
{
    public interface IGuideProcessor
    {
        IReadOnlyCollection<ProcessedCustomFormatData> CustomFormats { get; }
        IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache { get; }
        bool IsLoaded { get; }
        Task ForceBuildGuideData(RadarrConfig config);
        Task<bool> BuildGuideData(RadarrConfig config);
        Task SaveToRadarr(RadarrConfig config);
    }
}
