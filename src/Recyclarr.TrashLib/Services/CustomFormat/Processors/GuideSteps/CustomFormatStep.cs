using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;

namespace Recyclarr.TrashLib.Services.CustomFormat.Processors.GuideSteps;

public class CustomFormatStep : ICustomFormatStep
{
    private readonly List<ProcessedCustomFormatData> _processedCustomFormats = new();
    private readonly List<TrashIdMapping> _deletedCustomFormatsInCache = new();

    public IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats => _processedCustomFormats;
    public IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache => _deletedCustomFormatsInCache;

    public void Process(
        IList<CustomFormatData> customFormatGuideData,
        IReadOnlyCollection<CustomFormatConfig> config,
        CustomFormatCache? cache)
    {
        var processedCfs = customFormatGuideData
            .Select(cf => ProcessCustomFormatData(cf, cache))
            .ToList();

        // For each ID listed under the `trash_ids` YML property, match it to an existing CF
        _processedCustomFormats.AddRange(config
            .SelectMany(c => c.TrashIds)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .Join(processedCfs,
                id => id,
                cf => cf.TrashId,
                (_, cf) => cf,
                StringComparer.InvariantCultureIgnoreCase));

        // Orphaned entries in cache represent custom formats we need to delete.
        ProcessDeletedCustomFormats(cache);
    }

    private static ProcessedCustomFormatData ProcessCustomFormatData(CustomFormatData cf,
        CustomFormatCache? cache)
    {
        var map = cache?.TrashIdMappings.FirstOrDefault(c => c.TrashId == cf.TrashId);
        return new ProcessedCustomFormatData(cf)
        {
            FormatId = map?.CustomFormatId ?? 0
        };
    }

    private void ProcessDeletedCustomFormats(CustomFormatCache? cache)
    {
        if (cache == null)
        {
            return;
        }

        // Delete if CF is in cache and not in the guide or config
        _deletedCustomFormatsInCache.AddRange(
            cache.TrashIdMappings.Where(map => ProcessedCustomFormats.All(cf => cf.TrashId != map.TrashId)));
    }
}
