using System.Collections.Generic;
using System.Threading.Tasks;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;

namespace Trash.Radarr.CustomFormat.Processors
{
    internal interface IGuideProcessor
    {
        IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats { get; }
        IReadOnlyCollection<string> CustomFormatsNotInGuide { get; }
        IReadOnlyCollection<ProcessedConfigData> ConfigData { get; }
        IDictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores { get; }
        IReadOnlyCollection<(string name, string trashId, string profileName)> CustomFormatsWithoutScore { get; }
        IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache { get; }
        List<(string, string)> CustomFormatsWithOutdatedNames { get; }
        Dictionary<string, List<ProcessedCustomFormatData>> DuplicatedCustomFormats { get; }

        Task BuildGuideData(IReadOnlyList<CustomFormatConfig> config, CustomFormatCache? cache);
        void Reset();
    }
}
