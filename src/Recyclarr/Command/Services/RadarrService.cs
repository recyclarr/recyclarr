using Recyclarr.Command.Helpers;
using Recyclarr.Config;
using Serilog;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat;
using TrashLib.Radarr.QualityDefinition;

namespace Recyclarr.Command.Services;

public class RadarrService : ServiceBase<IRadarrCommand>
{
    private readonly IConfigurationLoader<RadarrConfiguration> _configLoader;
    private readonly Func<ICustomFormatUpdater> _customFormatUpdaterFactory;
    private readonly ILogger _log;
    private readonly Func<IRadarrQualityDefinitionUpdater> _qualityUpdaterFactory;

    public RadarrService(
        ILogger log,
        IServiceInitialization serviceInitialization,
        IConfigurationLoader<RadarrConfiguration> configLoader,
        Func<IRadarrQualityDefinitionUpdater> qualityUpdaterFactory,
        Func<ICustomFormatUpdater> customFormatUpdaterFactory)
        : base(log, serviceInitialization)
    {
        _log = log;
        _configLoader = configLoader;
        _qualityUpdaterFactory = qualityUpdaterFactory;
        _customFormatUpdaterFactory = customFormatUpdaterFactory;
    }

    protected override async Task Process(IRadarrCommand cmd)
    {
        foreach (var config in _configLoader.LoadMany(cmd.Config, "radarr"))
        {
            _log.Information("Processing server {Url}", config.BaseUrl);

            if (config.QualityDefinition != null)
            {
                await _qualityUpdaterFactory().Process(cmd.Preview, config);
            }

            if (config.CustomFormats.Count > 0)
            {
                await _customFormatUpdaterFactory().Process(cmd.Preview, config);
            }
        }
    }
}
