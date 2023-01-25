using Recyclarr.TrashLib.Services.CustomFormat;
using Recyclarr.TrashLib.Services.QualitySize;
using Recyclarr.TrashLib.Services.Radarr;
using Recyclarr.TrashLib.Services.Radarr.Config;

namespace Recyclarr.TrashLib.Services.Processors;

public class RadarrProcessor : IServiceProcessor<RadarrConfiguration>
{
    private readonly ILogger _log;
    private readonly ICustomFormatUpdater _cfUpdater;
    private readonly IQualitySizeUpdater _qualityUpdater;
    private readonly RadarrGuideService _guideService;

    public RadarrProcessor(
        ILogger log,
        ICustomFormatUpdater cfUpdater,
        IQualitySizeUpdater qualityUpdater,
        RadarrGuideService guideService)
    {
        _log = log;
        _cfUpdater = cfUpdater;
        _qualityUpdater = qualityUpdater;
        _guideService = guideService;
    }

    public async Task Process(RadarrConfiguration config, ISyncSettings settings)
    {
        var didWork = false;

        if (config.QualityDefinition != null)
        {
            await _qualityUpdater.Process(settings.Preview, config.QualityDefinition, _guideService);
            didWork = true;
        }

        if (config.CustomFormats.Count > 0)
        {
            await _cfUpdater.Process(settings.Preview, config.CustomFormats, _guideService);
            didWork = true;
        }

        if (!didWork)
        {
            _log.Information("Nothing to do");
        }
    }
}
