using System.Diagnostics.CodeAnalysis;
using Flurl.Http;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Services.Radarr.Config;
using Recyclarr.TrashLib.Services.Sonarr.Config;
using Spectre.Console;

namespace Recyclarr.TrashLib.Services.Processors;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
public class SyncProcessor : ISyncProcessor
{
    private readonly IAnsiConsole _console;
    private readonly ILogger _log;
    private readonly IConfigurationFinder _configFinder;
    private readonly IConfigurationLoader _configLoader;
    private readonly ServiceProcessorFactory _factory;

    public SyncProcessor(
        IAnsiConsole console,
        ILogger log,
        IConfigurationFinder configFinder,
        IConfigurationLoader configLoader,
        ServiceProcessorFactory factory)
    {
        _console = console;
        _log = log;
        _configFinder = configFinder;
        _configLoader = configLoader;
        _factory = factory;
    }

    public async Task<ExitStatus> ProcessConfigs(ISyncSettings settings)
    {
        var failureDetected = false;
        try
        {
            var configs = _configLoader.LoadMany(_configFinder.GetConfigFiles(settings.Configs));

            var invalidInstances = settings.Instances.Where(x => !configs.DoesConfigExist(x)).ToList();
            if (invalidInstances.Any())
            {
                _log.Warning("These instances do not exist: {Instances}", invalidInstances);
            }

            if (settings.Service is null or SupportedServices.Radarr)
            {
                failureDetected |=
                    await ProcessService<RadarrConfiguration>(SupportedServices.Radarr, settings, configs);
            }

            if (settings.Service is null or SupportedServices.Sonarr)
            {
                failureDetected |=
                    await ProcessService<SonarrConfiguration>(SupportedServices.Sonarr, settings, configs);
            }
        }
        catch (Exception e)
        {
            HandleException(e);
            failureDetected = true;
        }

        return failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
    }

    private async Task<bool> ProcessService<TConfig>(
        SupportedServices service, ISyncSettings settings, IConfigRegistry configs)
        where TConfig : ServiceConfiguration
    {
        var serviceConfigs = configs.GetConfigsOfType<TConfig>(service);

        // If any config names are null, that means user specified array-style (deprecated) instances.
        if (serviceConfigs.Any(x => x.InstanceName is null))
        {
            _log.Warning(
                "Found array-style list of instances instead of named-style. " +
                "Array-style lists of Sonarr/Radarr instances are deprecated");
        }

        var failureDetected = false;

        foreach (var config in serviceConfigs)
        {
            try
            {
                if (settings.Instances.Count > 0 &&
                    !settings.Instances.Any(i => i.EqualsIgnoreCase(config.InstanceName)))
                {
                    _log.Debug("Skipping instance {InstanceName} because it doesn't match what the user specified",
                        config.InstanceName);

                    continue;
                }

                PrintProcessingHeader(service.ToString(), config);
                using var processor = _factory.CreateProcessor<TConfig>(config);
                await processor.Value.Process(config, settings);
            }
            catch (Exception e)
            {
                HandleException(e);
                failureDetected = true;
            }
        }

        return failureDetected;
    }

    private void HandleException(Exception e)
    {
        switch (e)
        {
            case GitCmdException e2:
                _log.Error(e2, "Non-zero exit code {ExitCode} while executing Git command: {Error}",
                    e2.ExitCode, e2.Error);
                break;

            case FlurlHttpException e2:
                _log.Error("HTTP error: {Message}", e2.SanitizedExceptionMessage());
                break;

            default:
                _log.Error(e, "Exception");
                break;
        }
    }

    private void PrintProcessingHeader(string serverName, ServiceConfiguration config)
    {
        var instanceName = config.InstanceName ?? FlurlLogging.SanitizeUrl(config.BaseUrl);

        _console.WriteLine($@"
===========================================
Processing {serverName} Server: [{instanceName}]
===========================================
");

        _log.Debug("Processing {Server} server {Name}", serverName, instanceName);
    }
}
