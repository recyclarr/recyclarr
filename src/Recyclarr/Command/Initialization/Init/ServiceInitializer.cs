using Common.Networking;
using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using TrashLib;
using TrashLib.Config.Settings;
using TrashLib.Extensions;
using TrashLib.Repo;

namespace Recyclarr.Command.Initialization.Init;

internal class ServiceInitializer : IServiceInitializer
{
    private readonly ILogger _log;
    private readonly LoggingLevelSwitch _loggingLevelSwitch;
    private readonly ISettingsPersister _settingsPersister;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IRepoUpdater _repoUpdater;
    private readonly IConfigurationFinder _configFinder;

    public ServiceInitializer(
        ILogger log,
        LoggingLevelSwitch loggingLevelSwitch,
        ISettingsPersister settingsPersister,
        ISettingsProvider settingsProvider,
        IRepoUpdater repoUpdater,
        IConfigurationFinder configFinder)
    {
        _log = log;
        _loggingLevelSwitch = loggingLevelSwitch;
        _settingsPersister = settingsPersister;
        _settingsProvider = settingsProvider;
        _repoUpdater = repoUpdater;
        _configFinder = configFinder;
    }

    public void Initialize(ServiceCommand cmd)
    {
        // Must happen first because everything can use the logger.
        _loggingLevelSwitch.MinimumLevel = cmd.Debug ? LogEventLevel.Debug : LogEventLevel.Information;

        // Has to happen right after logging because stuff below may use settings.
        _settingsPersister.Load();

        SetupHttp();
        _repoUpdater.UpdateRepo();

        if (!cmd.Config.Any())
        {
            cmd.Config = new[] {_configFinder.FindConfigPath()};
        }
    }

    private void SetupHttp()
    {
        FlurlHttp.Configure(settings =>
        {
            var jsonSettings = new JsonSerializerSettings
            {
                // This is important. If any DTOs are missing members, say, if Radarr or Sonarr adds one in a future
                // version, this needs to fail to indicate that a software change is required. Otherwise, we lose
                // state between when we request settings, and re-apply them again with a few properties modified.
                MissingMemberHandling = MissingMemberHandling.Error,

                // This makes sure that null properties, such as maxSize and preferredSize in Radarr
                // Quality Definitions, do not get written out to JSON request bodies.
                NullValueHandling = NullValueHandling.Ignore
            };

            settings.JsonSerializer = new NewtonsoftJsonSerializer(jsonSettings);
            FlurlLogging.SetupLogging(settings, _log);

            if (!_settingsProvider.Settings.EnableSslCertificateValidation)
            {
                _log.Warning(
                    "Security Risk: Certificate validation is being DISABLED because setting `enable_ssl_certificate_validation` is set to `false`");
                settings.HttpClientFactory = new UntrustedCertClientFactory();
            }
        });
    }
}
