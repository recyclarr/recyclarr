using Recyclarr.TrashLib.Services.CustomFormat;
using Recyclarr.TrashLib.Services.QualitySize;
using Recyclarr.TrashLib.Services.Sonarr;
using Recyclarr.TrashLib.Services.Sonarr.Capabilities;
using Recyclarr.TrashLib.Services.Sonarr.Config;
using Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile;

namespace Recyclarr.TrashLib.Services.Processors;

public class SonarrProcessor : IServiceProcessor<SonarrConfiguration>
{
    private readonly ILogger _log;
    private readonly ICustomFormatUpdater _cfUpdater;
    private readonly IQualitySizeUpdater _qualityUpdater;
    private readonly SonarrGuideService _guideService;
    private readonly IReleaseProfileUpdater _profileUpdater;
    private readonly SonarrCapabilityEnforcer _compatibilityEnforcer;

    public SonarrProcessor(
        ILogger log,
        ICustomFormatUpdater cfUpdater,
        IQualitySizeUpdater qualityUpdater,
        SonarrGuideService guideService,
        IReleaseProfileUpdater profileUpdater,
        SonarrCapabilityEnforcer compatibilityEnforcer)
    {
        _log = log;
        _cfUpdater = cfUpdater;
        _qualityUpdater = qualityUpdater;
        _guideService = guideService;
        _profileUpdater = profileUpdater;
        _compatibilityEnforcer = compatibilityEnforcer;
    }

    public async Task Process(SonarrConfiguration config, ISyncSettings settings)
    {
        // Any compatibility failures will be thrown as exceptions
        _compatibilityEnforcer.Check(config);

        var didWork = false;

        if (config.ReleaseProfiles.Count > 0)
        {
            await _profileUpdater.Process(settings.Preview, config);
            didWork = true;
        }

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
