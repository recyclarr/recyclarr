using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Guide;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;
using TrashLib.Radarr.CustomFormat.Processors.GuideSteps;

namespace TrashLib.Radarr.CustomFormat.Processors;

public interface IGuideProcessorSteps
{
    ICustomFormatStep CustomFormat { get; }
    IConfigStep Config { get; }
    IQualityProfileStep QualityProfile { get; }
}

internal class GuideProcessor : IGuideProcessor
{
    private readonly IRadarrGuideService _guideService;
    private readonly Func<IGuideProcessorSteps> _stepsFactory;
    private IList<string>? _guideCustomFormatJson;
    private IGuideProcessorSteps _steps;

    public GuideProcessor(IRadarrGuideService guideService, Func<IGuideProcessorSteps> stepsFactory)
    {
        _guideService = guideService;
        _stepsFactory = stepsFactory;
        _steps = stepsFactory();
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

    public IReadOnlyCollection<TrashIdMapping> DeletedCustomFormatsInCache
        => _steps.CustomFormat.DeletedCustomFormatsInCache;

    public IReadOnlyCollection<(string, string)> CustomFormatsWithOutdatedNames
        => _steps.CustomFormat.CustomFormatsWithOutdatedNames;

    public IDictionary<string, List<ProcessedCustomFormatData>> DuplicatedCustomFormats
        => _steps.CustomFormat.DuplicatedCustomFormats;

    public Task BuildGuideDataAsync(IReadOnlyCollection<CustomFormatConfig> config, CustomFormatCache? cache)
    {
        if (_guideCustomFormatJson == null)
        {
            _guideCustomFormatJson = _guideService.GetCustomFormatJson().ToList();
        }

        // Step 1: Process and filter the custom formats from the guide.
        // Custom formats in the guide not mentioned in the config are filtered out.
        _steps.CustomFormat.Process(_guideCustomFormatJson, config, cache);

        // todo: Process cache entries that do not exist in the guide. Those should be deleted
        // This might get taken care of when we rebuild the cache based on what is actually updated when
        // we call the Radarr API

        // Step 2: Use the processed custom formats from step 1 to process the configuration.
        // CFs in config not in the guide are filtered out.
        // Actual CF objects are associated to the quality profile objects to reduce lookups
        _steps.Config.Process(_steps.CustomFormat.ProcessedCustomFormats, config);

        // Step 3: Use the processed config (which contains processed CFs) to process the quality profile scores.
        // Score precedence logic is utilized here to decide the CF score per profile (same CF can actually have
        // different scores depending on which profile it goes into).
        _steps.QualityProfile.Process(_steps.Config.ConfigData);

        return Task.CompletedTask;
    }

    public void Reset()
    {
        _steps = _stepsFactory();
    }
}
