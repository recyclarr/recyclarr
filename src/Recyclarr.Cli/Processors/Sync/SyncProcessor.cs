using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Flurl.Http;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Compatibility;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Parsing.ErrorHandling;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.ExceptionTypes;
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
    private readonly ServiceAgnosticCapabilityEnforcer _capabilityEnforcer;
    private readonly IFileSystem _fs;

    public SyncProcessor(
        IAnsiConsole console,
        ILogger log,
        IConfigurationFinder configFinder,
        IConfigurationLoader configLoader,
        SyncPipelineExecutor pipelines,
        ServiceAgnosticCapabilityEnforcer capabilityEnforcer,
        IFileSystem fs)
    {
        _console = console;
        _log = log;
        _configFinder = configFinder;
        _configLoader = configLoader;
        _pipelines = pipelines;
        _capabilityEnforcer = capabilityEnforcer;
        _fs = fs;
    }

    public async Task<ExitStatus> ProcessConfigs(ISyncSettings settings)
    {
        bool failureDetected;
        try
        {
            var configFiles = settings.Configs
                .Select(x => _fs.FileInfo.New(x))
                .ToLookup(x => x.Exists);

            if (configFiles[false].Any())
            {
                foreach (var file in configFiles[false])
                {
                    _log.Error("Manually-specified configuration file does not exist: {File}", file);
                }

                _log.Error("Exiting due to non-existent configuration files");
                return ExitStatus.Failed;
            }

            var configs = LoadAndFilterConfigs(_configFinder.GetConfigFiles(configFiles[true].ToList()), settings);

            failureDetected = await ProcessService(settings, configs);
        }
        catch (Exception e)
        {
            await HandleException(e);
            failureDetected = true;
        }

        return failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
    }

    private IEnumerable<IServiceConfiguration> LoadAndFilterConfigs(
        IEnumerable<IFileInfo> configs,
        ISyncSettings settings)
    {
        var loadedConfigs = configs.SelectMany(x => _configLoader.Load(x)).ToList();

        var invalidInstances = settings.GetInvalidInstanceNames(loadedConfigs).ToList();
        if (invalidInstances.Any())
        {
            throw new InvalidInstancesException(invalidInstances);
        }

        var splitInstances = loadedConfigs.GetSplitInstances().ToList();
        if (splitInstances.Any())
        {
            throw new SplitInstancesException(splitInstances);
        }

        return loadedConfigs.GetConfigsBasedOnSettings(settings);
    }

    private async Task<bool> ProcessService(ISyncSettings settings, IEnumerable<IServiceConfiguration> configs)
    {
        var failureDetected = false;

        foreach (var config in configs)
        {
            try
            {
                PrintProcessingHeader(config.ServiceType, config);
                await _capabilityEnforcer.Check(config);
                await _pipelines.Process(settings, config);
            }
            catch (Exception e)
            {
                await HandleException(e);
                failureDetected = true;
            }
        }

        return failureDetected;
    }

    private async Task HandleException(Exception sourceException)
    {
        switch (sourceException)
        {
            case GitCmdException e:
                _log.Error(e, "Non-zero exit code {ExitCode} while executing Git command: {Error}",
                    e.ExitCode, e.Error);
                break;

            case FlurlHttpException e:
                _log.Error("HTTP error: {Message}", e.SanitizedExceptionMessage());
                foreach (var error in await GetValidationErrorsAsync(e))
                {
                    _log.Error("Reason: {Error}", error);
                }

                break;

            case NoConfigurationFilesException:
                _log.Error("No configuration files found");
                break;

            case InvalidInstancesException e:
                _log.Error("The following instances do not exist: {Names}", e.InstanceNames);
                break;

            case SplitInstancesException e:
                _log.Error("The following configs share the same `base_url`, which isn't allowed: {Instances}",
                    e.InstanceNames);
                _log.Error(
                    "Consolidate the config files manually to fix. " +
                    "See: https://recyclarr.dev/wiki/yaml/config-examples/#merge-single-instance");
                break;

            default:
                throw sourceException;
        }
    }

    private static async Task<IReadOnlyCollection<string>> GetValidationErrorsAsync(FlurlHttpException e)
    {
        var response = await e.GetResponseJsonAsync<List<dynamic>>();
        if (response is null)
        {
            return Array.Empty<string>();
        }

        return response
            .Select(x => (string) x.errorMessage)
            .NotNull(x => !string.IsNullOrEmpty(x))
            .ToList();
    }

    private void PrintProcessingHeader(SupportedServices serviceType, IServiceConfiguration config)
    {
        var instanceName = config.InstanceName;

        _console.WriteLine(
            $"""

             ===========================================
             Processing {serviceType} Server: [{instanceName}]
             ===========================================

             """);

        _log.Debug("Processing {Server} server {Name}", serviceType, instanceName);
    }
}
