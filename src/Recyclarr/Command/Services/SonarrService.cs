using System.IO.Abstractions;
using CliFx.Exceptions;
using Recyclarr.Config;
using Serilog;
using TrashLib;
using TrashLib.Extensions;
using TrashLib.Sonarr;
using TrashLib.Sonarr.Config;
using TrashLib.Sonarr.QualityDefinition;
using TrashLib.Sonarr.ReleaseProfile;

namespace Recyclarr.Command.Services;

public class SonarrService : ServiceBase<ISonarrCommand>
{
    private readonly ILogger _log;
    private readonly IConfigurationLoader<SonarrConfiguration> _configLoader;
    private readonly Func<IReleaseProfileUpdater> _profileUpdaterFactory;
    private readonly Func<ISonarrQualityDefinitionUpdater> _qualityUpdaterFactory;
    private readonly IReleaseProfileLister _lister;
    private readonly IFileSystem _fs;
    private readonly IAppPaths _paths;

    public SonarrService(
        ILogger log,
        IConfigurationLoader<SonarrConfiguration> configLoader,
        Func<IReleaseProfileUpdater> profileUpdaterFactory,
        Func<ISonarrQualityDefinitionUpdater> qualityUpdaterFactory,
        IReleaseProfileLister lister,
        IFileSystem fs,
        IAppPaths paths)
    {
        _log = log;
        _configLoader = configLoader;
        _profileUpdaterFactory = profileUpdaterFactory;
        _qualityUpdaterFactory = qualityUpdaterFactory;
        _lister = lister;
        _fs = fs;
        _paths = paths;
    }

    public string DefaultCacheStoragePath => _fs.Path.Combine(_paths.CacheDirectory, "sonarr");

    protected override async Task Process(ISonarrCommand cmd)
    {
        if (cmd.ListReleaseProfiles)
        {
            _lister.ListReleaseProfiles();
            return;
        }

        if (cmd.ListTerms != "empty")
        {
            if (!string.IsNullOrEmpty(cmd.ListTerms))
            {
                _lister.ListTerms(cmd.ListTerms);
            }
            else
            {
                throw new CommandException(
                    "The --list-terms option was specified without a Release Profile Trash ID specified");
            }

            return;
        }

        foreach (var config in _configLoader.LoadMany(cmd.Config, "sonarr"))
        {
            _log.Information("Processing server {Url}", FlurlLogging.SanitizeUrl(config.BaseUrl));

            if (config.ReleaseProfiles.Count > 0)
            {
                await _profileUpdaterFactory().Process(cmd.Preview, config);
            }

            if (config.QualityDefinition.HasValue)
            {
                await _qualityUpdaterFactory().Process(cmd.Preview, config);
            }
        }
    }
}
