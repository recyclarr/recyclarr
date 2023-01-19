using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Services.CustomFormat;
using Recyclarr.TrashLib.Services.QualitySize;
using Recyclarr.TrashLib.Services.Radarr.Config;

namespace Recyclarr.TrashLib.Services.Processors;

public class RadarrProcessor : IServiceProcessor
{
    private readonly ILogger _log;
    private readonly ICustomFormatUpdater _cfUpdater;
    private readonly IQualitySizeUpdater _qualityUpdater;
    private readonly RadarrConfiguration _config;

    public RadarrProcessor(
        ILogger log,
        ICustomFormatUpdater cfUpdater,
        IQualitySizeUpdater qualityUpdater,
        RadarrConfiguration config)
    {
        _log = log;
        _cfUpdater = cfUpdater;
        _qualityUpdater = qualityUpdater;
        _config = config;
    }

    public async Task Process(ISyncSettings settings)
    {
        var didWork = false;

        if (_config.QualityDefinition != null)
        {
            await _qualityUpdater.Process(settings.Preview, _config.QualityDefinition, SupportedServices.Radarr);
            didWork = true;
        }

        if (_config.CustomFormats.Count > 0)
        {
            await _cfUpdater.Process(settings.Preview, _config.CustomFormats, SupportedServices.Radarr);
            didWork = true;
        }

        if (!didWork)
        {
            _log.Information("Nothing to do");
        }
    }
}
