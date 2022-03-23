using CliFx.Attributes;
using Flurl.Http;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Trash.Config;
using TrashLib.Config.Settings;
using TrashLib.Extensions;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat;
using TrashLib.Radarr.QualityDefinition;

namespace Trash.Command;

[Command("radarr", Description = "Perform operations on a Radarr instance")]
[UsedImplicitly]
public class RadarrCommand : ServiceCommand
{
    private readonly IConfigurationLoader<RadarrConfiguration> _configLoader;
    private readonly Func<ICustomFormatUpdater> _customFormatUpdaterFactory;
    private readonly ILogger _log;
    private readonly Func<IRadarrQualityDefinitionUpdater> _qualityUpdaterFactory;

    public RadarrCommand(
        ILogger log,
        LoggingLevelSwitch loggingLevelSwitch,
        ILogJanitor logJanitor,
        ISettingsPersister settingsPersister,
        ISettingsProvider settingsProvider,
        IConfigurationLoader<RadarrConfiguration> configLoader,
        Func<IRadarrQualityDefinitionUpdater> qualityUpdaterFactory,
        Func<ICustomFormatUpdater> customFormatUpdaterFactory)
        : base(log, loggingLevelSwitch, logJanitor, settingsPersister, settingsProvider)
    {
        _log = log;
        _configLoader = configLoader;
        _qualityUpdaterFactory = qualityUpdaterFactory;
        _customFormatUpdaterFactory = customFormatUpdaterFactory;
    }

    public override string CacheStoragePath { get; } =
        Path.Combine(AppPaths.AppDataPath, "cache", "radarr");

    public override async Task Process()
    {
        try
        {
            foreach (var config in _configLoader.LoadMany(Config, "radarr"))
            {
                _log.Information("Processing server {Url}", config.BaseUrl);

                if (config.QualityDefinition != null)
                {
                    await _qualityUpdaterFactory().Process(Preview, config);
                }

                if (config.CustomFormats.Count > 0)
                {
                    await _customFormatUpdaterFactory().Process(Preview, config);
                }
            }
        }
        catch (FlurlHttpException e)
        {
            _log.Error("HTTP error while communicating with Radarr: {Msg}", e.SanitizedExceptionMessage());
            ExitDueToFailure();
        }
    }
}
