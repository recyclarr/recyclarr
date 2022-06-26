using System.Text;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Common.Networking;
using Flurl.Http;
using Flurl.Http.Configuration;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Recyclarr.Command.Helpers;
using Recyclarr.Migration;
using Serilog;
using TrashLib;
using TrashLib.Config.Settings;
using TrashLib.Extensions;
using TrashLib.Repo;
using YamlDotNet.Core;

namespace Recyclarr.Command;

public abstract class ServiceCommand : BaseCommand, IServiceCommand
{
    [CommandOption("preview", 'p', Description =
        "Only display the processed markdown results without making any API calls.")]
    public bool Preview { get; [UsedImplicitly] set; } = false;

    [CommandOption("config", 'c', Description =
        "One or more YAML config files to use. All configs will be used and settings are additive. " +
        "If not specified, the script will look for `recyclarr.yml` in the same directory as the executable.")]
    public ICollection<string> Config { get; [UsedImplicitly] set; } = new List<string>();

    [CommandOption("app-data", Description =
        "Explicitly specify the location of the recyclarr application data directory. " +
        "Mainly for usage in Docker; not recommended for normal use.")]
    public override string? AppDataDirectory { get; [UsedImplicitly] set; }

    public abstract string Name { get; }

    public sealed override async ValueTask ExecuteAsync(IConsole console)
    {
        try
        {
            await base.ExecuteAsync(console);
        }
        catch (YamlException e)
        {
            var message = e.InnerException is not null ? e.InnerException.Message : e.Message;
            var msg = new StringBuilder();
            msg.AppendLine($"Found Unrecognized YAML Property: {message}");
            msg.AppendLine("Please remove the property quoted in the above message from your YAML file");
            msg.AppendLine("Exiting due to invalid configuration");
            throw new CommandException(msg.ToString());
        }
        catch (FlurlHttpException e)
        {
            throw new CommandException(
                $"HTTP error while communicating with {Name}: {e.SanitizedExceptionMessage()}");
        }
        catch (Exception e) when (e is not CommandException)
        {
            throw new CommandException(e.ToString());
        }
    }

    public override Task Process(IServiceLocatorProxy container)
    {
        var log = container.Resolve<ILogger>();
        var settingsPersister = container.Resolve<ISettingsPersister>();
        var settingsProvider = container.Resolve<ISettingsProvider>();
        var repoUpdater = container.Resolve<IRepoUpdater>();
        var configFinder = container.Resolve<IConfigurationFinder>();
        var commandProvider = container.Resolve<IActiveServiceCommandProvider>();
        var migration = container.Resolve<IMigrationExecutor>();

        commandProvider.ActiveCommand = this;

        // Will throw if migration is required, otherwise just a warning is issued.
        migration.CheckNeededMigrations();

        // Stuff below may use settings.
        settingsPersister.Load();

        SetupHttp(log, settingsProvider);
        repoUpdater.UpdateRepo();

        if (!Config.Any())
        {
            Config = new[] {configFinder.FindConfigPath().FullName};
        }

        return Task.CompletedTask;
    }

    private void SetupHttp(ILogger log, ISettingsProvider settingsProvider)
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
            FlurlLogging.SetupLogging(settings, log);

            if (!settingsProvider.Settings.EnableSslCertificateValidation)
            {
                log.Warning(
                    "Security Risk: Certificate validation is being DISABLED because setting " +
                    "`enable_ssl_certificate_validation` is set to `false`");
                settings.HttpClientFactory = new UntrustedCertClientFactory();
            }
        });
    }
}
