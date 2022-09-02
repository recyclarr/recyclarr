using Common.Extensions;
using TrashLib.Config.Services;
using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;

namespace TrashLib.Services.CustomFormat.Processors.GuideSteps;

public class CustomFormatStep : ICustomFormatStep
{
    private readonly List<(string, string)> _customFormatsWithOutdatedNames = new();
    private readonly List<ProcessedCustomFormatData> _processedCustomFormats = new();
    private readonly List<TrashIdMapping> _deletedCustomFormatsInCache = new();
    private readonly Dictionary<string, List<ProcessedCustomFormatData>> _duplicatedCustomFormats = new();

    public IReadOnlyCollection<(string, string)> CustomFormatsWithOutdatedNames => _customFormatsWithOutdatedNames;
    public IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats => _processedCustomFormats;
    public IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache => _deletedCustomFormatsInCache;
    public IDictionary<string, List<ProcessedCustomFormatData>> DuplicatedCustomFormats => _duplicatedCustomFormats;

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
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Join(processedCfs,
                id => id,
                cf => cf.TrashId,
                (_, cf) => cf,
                StringComparer.InvariantCultureIgnoreCase));

        // Build a list of CF names under the `names` property in YAML. Exclude any names that
        // are already provided by the `trash_ids` property.
        var allConfigCfNames = config
            .SelectMany(c => c.Names)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Where(n => !ProcessedCustomFormats.Any(cf => cf.CacheAwareName.EqualsIgnoreCase(n)))
            .ToList();

        // Perform updates and deletions based on matches in the cache. Matches in the cache are by ID.
        foreach (var cf in processedCfs)
        {
            // Does the name of the CF in the guide match a name in the config? If yes, we keep it.
            var configName = allConfigCfNames.FirstOrDefault(n => n.EqualsIgnoreCase(cf.Name));
            if (configName != null)
            {
                if (cf.CacheEntry != null)
                {
                    // The cache entry might be using an old name. This will happen if:
                    // - A user has synced this CF before, AND
                    // - The name of the CF in the guide changed, AND
                    // - The user updated the name in their config to match the name in the guide.
                    cf.CacheEntry.CustomFormatName = cf.Name;
                }

                _processedCustomFormats.Add(cf);
                continue;
            }

            // Does the name of the CF in the cache match a name in the config? If yes, we keep it.
            configName = allConfigCfNames.FirstOrDefault(n => n.EqualsIgnoreCase(cf.CacheEntry?.CustomFormatName));
            if (configName != null)
            {
                // Config name is out of sync with the guide and should be updated
                _customFormatsWithOutdatedNames.Add((configName, cf.Name));
                _processedCustomFormats.Add(cf);
            }

            // If we get here, we can't find a match in the config using cache or guide name, so the user must have
            // removed it from their config. This will get marked for deletion later.
        }

        // Orphaned entries in cache represent custom formats we need to delete.
        ProcessDeletedCustomFormats(cache);

        // Check for multiple custom formats with the same name in the guide data (e.g. "DoVi")
        ProcessDuplicates();
    }

    private void ProcessDuplicates()
    {
        _duplicatedCustomFormats.Clear();
        _duplicatedCustomFormats.AddRange(ProcessedCustomFormats
            .GroupBy(cf => cf.Name)
            .Where(grp => grp.Count() > 1)
            .ToDictionary(grp => grp.Key, grp => grp.ToList()));

        _processedCustomFormats.RemoveAll(cf => DuplicatedCustomFormats.ContainsKey(cf.Name));
    }

    private static ProcessedCustomFormatData ProcessCustomFormatData(CustomFormatData cf,
        CustomFormatCache? cache)
    {
        return new ProcessedCustomFormatData(cf)
        {
            CacheEntry = cache?.TrashIdMappings.FirstOrDefault(c => c.TrashId == cf.TrashId)
        };
    }

    private void ProcessDeletedCustomFormats(CustomFormatCache? cache)
    {
        if (cache == null)
        {
            return;
        }

        static bool MatchCfInCache(ProcessedCustomFormatData cf, TrashIdMapping c)
            => cf.CacheEntry != null && cf.CacheEntry.TrashId == c.TrashId;

        // Delete if CF is in cache and not in the guide or config
        _deletedCustomFormatsInCache.AddRange(cache.TrashIdMappings
            .Where(c => !ProcessedCustomFormats.Any(cf => MatchCfInCache(cf, c))));
    }
}
