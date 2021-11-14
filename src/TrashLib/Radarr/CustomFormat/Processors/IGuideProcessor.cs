using System.Collections.Generic;
using System.Threading.Tasks;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Processors
{
    internal interface IGuideProcessor
    {
        IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats { get; }
        IReadOnlyCollection<string> CustomFormatsNotInGuide { get; }
        IReadOnlyCollection<ProcessedConfigData> ConfigData { get; }
        IDictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores { get; }
        IReadOnlyCollection<(string name, string trashId, string profileName)> CustomFormatsWithoutScore { get; }
        IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache { get; }
        IReadOnlyCollection<(string, string)> CustomFormatsWithOutdatedNames { get; }
        IDictionary<string, List<ProcessedCustomFormatData>> DuplicatedCustomFormats { get; }

        Task BuildGuideDataAsync(IReadOnlyCollection<CustomFormatConfig> config, CustomFormatCache? cache);
        void Reset();
    }
}
