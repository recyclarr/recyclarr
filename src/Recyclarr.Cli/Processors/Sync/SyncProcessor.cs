using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.ErrorHandling;
using Recyclarr.Common;
using Recyclarr.Compatibility;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
public class SyncProcessor : ISyncProcessor
{
    private readonly IAnsiConsole _console;
    private readonly ILogger _log;
    private readonly IConfigurationRegistry _configRegistry;
    private readonly SyncPipelineExecutor _pipelines;
    private readonly ServiceAgnosticCapabilityEnforcer _capabilityEnforcer;
    private readonly ConsoleExceptionHandler _exceptionHandler;

    public SyncProcessor(
        IAnsiConsole console,
        ILogger log,
        IConfigurationRegistry configRegistry,
        SyncPipelineExecutor pipelines,
        ServiceAgnosticCapabilityEnforcer capabilityEnforcer,
        ConsoleExceptionHandler exceptionHandler)
    {
        _console = console;
        _log = log;
        _configRegistry = configRegistry;
        _pipelines = pipelines;
        _capabilityEnforcer = capabilityEnforcer;
        _exceptionHandler = exceptionHandler;
    }

    public async Task<ExitStatus> ProcessConfigs(ISyncSettings settings)
    {
        bool failureDetected;
        try
        {
            var configs = _configRegistry.FindAndLoadConfigs(new ConfigFilterCriteria
            {
                ManualConfigFiles = settings.Configs,
                Instances = settings.Instances,
                Service = settings.Service
            });

            failureDetected = await ProcessService(settings, configs);
        }
        catch (Exception e)
        {
            if (!await _exceptionHandler.HandleException(e))
            {
                // This means we didn't handle the exception; rethrow it.
                throw;
            }

            failureDetected = true;
        }

        return failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
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
                _log.Information("Completed at {Date}", DateTime.Now);
            }
            catch (Exception e)
            {
                if (!await _exceptionHandler.HandleException(e))
                {
                    // This means we didn't handle the exception; rethrow it.
                    throw;
                }

                failureDetected = true;
            }
        }

        return failureDetected;
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
