using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.CustomFormat.Guide;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;
using Recyclarr.TrashLib.Services.CustomFormat.Processors.GuideSteps;

namespace Recyclarr.TrashLib.Services.CustomFormat.Processors;

public interface IGuideProcessorSteps
{
    ICustomFormatStep CustomFormat { get; }
    IConfigStep Config { get; }
    IQualityProfileStep QualityProfile { get; }
}

internal class GuideProcessor : IGuideProcessor
{
    private IList<CustomFormatData>? _guideCustomFormatJson;
    private readonly IGuideProcessorSteps _steps;
    private readonly ICustomFormatGuideService _guide;

    public GuideProcessor(
        IGuideProcessorSteps steps,
        ICustomFormatGuideService guide)
    {
        _steps = steps;
        _guide = guide;
    }

    public IReadOnlyCollection<ProcessedCustomFormatData> ProcessedCustomFormats
        => _steps.CustomFormat.ProcessedCustomFormats;

    public IReadOnlyCollection<string> CustomFormatsNotInGuide
        => _steps.Config.CustomFormatsNotInGuide;

    public IReadOnlyCollection<ProcessedConfigData> ConfigData
        => _steps.Config.ConfigData;

    public IDictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores
        => _steps.QualityProfile.ProfileScores;

    public IReadOnlyCollection<(string name, string trashId, string profileName)> CustomFormatsWithoutScore
        => _steps.QualityProfile.CustomFormatsWithoutScore;

    public IReadOnlyDictionary<string, Dictionary<string, HashSet<int>>> DuplicateScores
        => _steps.QualityProfile.DuplicateScores;

    public IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache
        => _steps.CustomFormat.DeletedCustomFormatsInCache;

    public Task BuildGuideDataAsync(
        IEnumerable<CustomFormatConfig> config,
        CustomFormatCache? cache,
        SupportedServices serviceType)
    {
        _guideCustomFormatJson ??= _guide.GetCustomFormatData(serviceType).ToList();

        var listOfConfigs = config.ToList();

        // Step 1: Process and filter the custom formats from the guide.
        // Custom formats in the guide not mentioned in the config are filtered out.
        _steps.CustomFormat.Process(_guideCustomFormatJson, listOfConfigs, cache);

        // todo: Process cache entries that do not exist in the guide. Those should be deleted
        // This might get taken care of when we rebuild the cache based on what is actually updated when
        // we call the Radarr API

        // Step 2: Use the processed custom formats from step 1 to process the configuration.
        // CFs in config not in the guide are filtered out.
        // Actual CF objects are associated to the quality profile objects to reduce lookups
        _steps.Config.Process(_steps.CustomFormat.ProcessedCustomFormats, listOfConfigs);

        // Step 3: Use the processed config (which contains processed CFs) to process the quality profile scores.
        // Score precedence logic is utilized here to decide the CF score per profile (same CF can actually have
        // different scores depending on which profile it goes into).
        _steps.QualityProfile.Process(_steps.Config.ConfigData);

        return Task.CompletedTask;
    }
}
