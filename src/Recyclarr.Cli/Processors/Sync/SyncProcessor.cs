using System.Diagnostics.CodeAnalysis;
using Flurl.Http;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Repo.VersionControl;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
public class SyncProcessor : ISyncProcessor
{
    private readonly IAnsiConsole _console;
    private readonly ILogger _log;
    private readonly IConfigurationFinder _configFinder;
    private readonly IConfigurationLoader _configLoader;
    private readonly SyncPipelineExecutor _pipelines;

    public SyncProcessor(
        IAnsiConsole console,
        ILogger log,
        IConfigurationFinder configFinder,
        IConfigurationLoader configLoader,
        SyncPipelineExecutor pipelines)
    {
        _console = console;
        _log = log;
        _configFinder = configFinder;
        _configLoader = configLoader;
        _pipelines = pipelines;
    }

    public async Task<ExitStatus> ProcessConfigs(ISyncSettings settings)
    {
        bool failureDetected;
        try
        {
            var configs = _configLoader.LoadMany(_configFinder.GetConfigFiles(settings.Configs));

            LogInvalidInstances(settings.Instances, configs);

            failureDetected = await ProcessService(settings, configs);
        }
        catch (Exception e)
        {
            HandleException(e);
            failureDetected = true;
        }

        return failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
    }

    private void LogInvalidInstances(IEnumerable<string>? instanceNames, ICollection<IServiceConfiguration> configs)
    {
        var invalidInstances = instanceNames?
            .Where(x => !configs.DoesConfigExist(x))
            .ToList();

        if (invalidInstances != null && invalidInstances.Any())
        {
            _log.Warning("These instances do not exist: {Instances}", invalidInstances);
        }
    }

    private async Task<bool> ProcessService(ISyncSettings settings, ICollection<IServiceConfiguration> configs)
    {
        var failureDetected = false;

        foreach (var config in configs.GetConfigsBasedOnSettings(settings))
        {
            try
            {
                PrintProcessingHeader(config.ServiceType, config);
                await _pipelines.Process(settings, config);
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

    private void PrintProcessingHeader(SupportedServices serviceType, IServiceConfiguration config)
    {
        var instanceName = config.InstanceName;

        _console.WriteLine($@"
===========================================
Processing {serviceType} Server: [{instanceName}]
===========================================
");

        _log.Debug("Processing {Server} server {Name}", serviceType, instanceName);
    }
}
