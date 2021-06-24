using System.Collections.Generic;
using System.Threading.Tasks;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace Recyclarr.Code.Radarr
{
    public interface IGuideProcessor
    {
        IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats { get; }
        IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache { get; }
        List<(string, string)> CustomFormatsWithOutdatedNames { get; }
        bool IsLoaded { get; }
        Task BuildGuideData(RadarrConfig config);
        Task SaveToRadarr(RadarrConfig config);
        Task ForceBuildGuideData(RadarrConfig config);
    }
}
