using CliFx.Attributes;
using Flurl.Http;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Trash.Config;
using TrashLib.Config.Settings;
using TrashLib.Repo;
using TrashLib.Sonarr.Config;
using TrashLib.Sonarr.QualityDefinition;
using TrashLib.Sonarr.ReleaseProfile;

namespace Trash.Command;

[Command("sonarr", Description = "Perform operations on a Sonarr instance")]
[UsedImplicitly]
public class SonarrCommand : ServiceCommand
{
    private readonly IConfigurationLoader<SonarrConfiguration> _configLoader;
    private readonly ILogger _log;
    private readonly Func<IReleaseProfileUpdater> _profileUpdaterFactory;
    private readonly Func<ISonarrQualityDefinitionUpdater> _qualityUpdaterFactory;

    public SonarrCommand(
        ILogger log,
        LoggingLevelSwitch loggingLevelSwitch,
        ILogJanitor logJanitor,
        ISettingsPersister settingsPersister,
        ISettingsProvider settingsProvider,
        IRepoUpdater repoUpdater,
        IConfigurationLoader<SonarrConfiguration> configLoader,
        Func<IReleaseProfileUpdater> profileUpdaterFactory,
        Func<ISonarrQualityDefinitionUpdater> qualityUpdaterFactory)
        : base(log, loggingLevelSwitch, logJanitor, settingsPersister, settingsProvider, repoUpdater)
    {
        _log = log;
        _configLoader = configLoader;
        _profileUpdaterFactory = profileUpdaterFactory;
        _qualityUpdaterFactory = qualityUpdaterFactory;
    }

    public override string CacheStoragePath { get; } =
        Path.Combine(AppPaths.AppDataPath, "cache", "sonarr");

    protected override async Task Process()
    {
        try
        {
            foreach (var config in _configLoader.LoadMany(Config, "sonarr"))
            {
                _log.Information("Processing server {Url}", config.BaseUrl);

                if (config.ReleaseProfiles.Count > 0)
                {
                    await _profileUpdaterFactory().Process(Preview, config);
                }

                if (config.QualityDefinition.HasValue)
                {
                    await _qualityUpdaterFactory().Process(Preview, config);
                }
            }
        }
        catch (FlurlHttpException e)
        {
            _log.Error(e, "HTTP error while communicating with Sonarr");
            ExitDueToFailure();
        }
    }
}
