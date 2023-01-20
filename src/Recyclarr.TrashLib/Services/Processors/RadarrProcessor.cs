using Recyclarr.TrashLib.Services.CustomFormat;
using Recyclarr.TrashLib.Services.QualitySize;
using Recyclarr.TrashLib.Services.Radarr.Config;

namespace Recyclarr.TrashLib.Services.Processors;

public class RadarrProcessor : IServiceProcessor
{
    private readonly ICustomFormatUpdater _cfUpdater;
    private readonly IQualitySizeUpdater _qualityUpdater;
    private readonly RadarrConfiguration _config;

    public RadarrProcessor(
        ICustomFormatUpdater cfUpdater,
        IQualitySizeUpdater qualityUpdater,
        RadarrConfiguration config)
    {
        _cfUpdater = cfUpdater;
        _qualityUpdater = qualityUpdater;
        _config = config;
    }

    public async Task Process(ISyncSettings settings)
    {
        if (_config.QualityDefinition != null)
        {
            await _qualityUpdater.Process(settings.Preview, _config);
        }

        if (_config.CustomFormats.Count > 0)
        {
            await _cfUpdater.Process(settings.Preview, _config);
        }
    }
}
