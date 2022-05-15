using CliFx.Exceptions;
using Recyclarr.Command.Helpers;
using Recyclarr.Config;
using Serilog;
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

    public SonarrService(
        ILogger log,
        IServiceInitialization serviceInitialization,
        IConfigurationLoader<SonarrConfiguration> configLoader,
        Func<IReleaseProfileUpdater> profileUpdaterFactory,
        Func<ISonarrQualityDefinitionUpdater> qualityUpdaterFactory,
        IReleaseProfileLister lister)
        : base(log, serviceInitialization)
    {
        _log = log;
        _configLoader = configLoader;
        _profileUpdaterFactory = profileUpdaterFactory;
        _qualityUpdaterFactory = qualityUpdaterFactory;
        _lister = lister;
    }

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
            _log.Information("Processing server {Url}", config.BaseUrl);

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
