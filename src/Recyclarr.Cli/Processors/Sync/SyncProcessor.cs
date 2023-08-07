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

            var configs = _configLoader.LoadMany(_configFinder.GetConfigFiles(configFiles[true].ToList()));

            LogInvalidInstances(settings.Instances, configs);

            failureDetected = await ProcessService(settings, configs);
        }
        catch (Exception e)
        {
            await HandleException(e);
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

    private async Task HandleException(Exception e)
    {
        switch (e)
        {
            case GitCmdException e2:
                _log.Error(e2, "Non-zero exit code {ExitCode} while executing Git command: {Error}",
                    e2.ExitCode, e2.Error);
                break;

            case FlurlHttpException e2:
                _log.Error("HTTP error: {Message}", e2.SanitizedExceptionMessage());
                foreach (var error in await GetValidationErrorsAsync(e2))
                {
                    _log.Error("Reason: {Error}", error);
                }

                break;

            case NoConfigurationFilesException:
                _log.Error("No configuration files found");
                break;

            default:
                throw e;
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
