using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Flurl.Http;
using Flurl.Http.Configuration;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using TrashLib.Extensions;
using YamlDotNet.Core;

namespace Trash.Command;

public abstract class ServiceCommand : ICommand, IServiceCommand
{
    private readonly ILogger _log;
    private readonly LoggingLevelSwitch _loggingLevelSwitch;
    private readonly ILogJanitor _logJanitor;

    protected ServiceCommand(
        ILogger log,
        LoggingLevelSwitch loggingLevelSwitch,
        ILogJanitor logJanitor)
    {
        _loggingLevelSwitch = loggingLevelSwitch;
        _logJanitor = logJanitor;
        _log = log;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        SetupLogging();
        SetupHttp();

        try
        {
            await Process();
        }
        catch (YamlException e)
        {
            var inner = e.InnerException;
            if (inner == null)
            {
                throw;
            }

            _log.Error("Found Unrecognized YAML Property: {ErrorMsg}", inner.Message);
            _log.Error("Please remove the property quoted in the above message from your YAML file");
            throw new CommandException("Exiting due to invalid configuration");
        }
        catch (Exception e) when (e is not CommandException)
        {
            _log.Error(e, "Unrecoverable Exception");
            ExitDueToFailure();
        }
        finally
        {
            CleanupOldLogFiles();
        }
    }

    [CommandOption("preview", 'p', Description =
        "Only display the processed markdown results without making any API calls.")]
    public bool Preview { get; [UsedImplicitly] set; } = false;

    [CommandOption("debug", 'd', Description =
        "Display additional logs useful for development/debug purposes.")]
    public bool Debug { get; [UsedImplicitly] set; } = false;

    [CommandOption("config", 'c', Description =
        "One or more YAML config files to use. All configs will be used and settings are additive. " +
        "If not specified, the script will look for `trash.yml` in the same directory as the executable.")]
    public ICollection<string> Config { get; [UsedImplicitly] set; } =
        new List<string> {AppPaths.DefaultConfigPath};

    public abstract string CacheStoragePath { get; }

    private void CleanupOldLogFiles()
    {
        _logJanitor.DeleteOldestLogFiles(20);
    }

    private void SetupLogging()
    {
        _loggingLevelSwitch.MinimumLevel =
            Debug ? LogEventLevel.Debug : LogEventLevel.Information;
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
        });
    }

    public abstract Task Process();

    protected static void ExitDueToFailure()
    {
        throw new CommandException("Exiting due to previous exception");
    }
}
