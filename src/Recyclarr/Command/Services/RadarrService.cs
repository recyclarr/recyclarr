using System.IO.Abstractions;
using Recyclarr.Config;
using Serilog;
using TrashLib;
using TrashLib.Extensions;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat;
using TrashLib.Radarr.QualityDefinition;

namespace Recyclarr.Command.Services;

public class RadarrService : ServiceBase<IRadarrCommand>
{
    private readonly IConfigurationLoader<RadarrConfiguration> _configLoader;
    private readonly Func<ICustomFormatUpdater> _customFormatUpdaterFactory;
    private readonly ICustomFormatLister _lister;
    private readonly IFileSystem _fs;
    private readonly IAppPaths _paths;
    private readonly ILogger _log;
    private readonly Func<IRadarrQualityDefinitionUpdater> _qualityUpdaterFactory;

    public RadarrService(
        ILogger log,
        IConfigurationLoader<RadarrConfiguration> configLoader,
        Func<IRadarrQualityDefinitionUpdater> qualityUpdaterFactory,
        Func<ICustomFormatUpdater> customFormatUpdaterFactory,
        ICustomFormatLister lister,
        IFileSystem fs,
        IAppPaths paths)
    {
        _log = log;
        _configLoader = configLoader;
        _qualityUpdaterFactory = qualityUpdaterFactory;
        _customFormatUpdaterFactory = customFormatUpdaterFactory;
        _lister = lister;
        _fs = fs;
        _paths = paths;
    }

    public string DefaultCacheStoragePath => _fs.Path.Combine(_paths.CacheDirectory, "radarr");

    protected override async Task Process(IRadarrCommand cmd)
    {
        if (cmd.ListCustomFormats)
        {
            _lister.ListCustomFormats();
            return;
        }

        foreach (var config in _configLoader.LoadMany(cmd.Config, "radarr"))
        {
            _log.Information("Processing server {Url}", FlurlLogging.SanitizeUrl(config.BaseUrl));

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
