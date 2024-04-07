using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.ErrorHandling;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;

namespace Recyclarr.Cli.Processors.Sync;

public class SyncBasedConfigurationScope(ILifetimeScope scope) : ConfigurationScope(scope)
{
    public SyncPipelineExecutor Pipelines { get; } = scope.Resolve<SyncPipelineExecutor>();
}

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
public class SyncProcessor(
    IConfigurationRegistry configRegistry,
    ConfigurationScopeFactory configScopeFactory,
    ConsoleExceptionHandler exceptionHandler,
    NotificationService notify)
    : ISyncProcessor
{
    public async Task<ExitStatus> Process(ISyncSettings settings)
    {
        var result = await ProcessConfigs(settings);
        await notify.SendNotification(result != ExitStatus.Failed);
        return result;
    }

    private async Task<ExitStatus> ProcessConfigs(ISyncSettings settings)
    {
        bool failureDetected;
        try
        {
            var configs = configRegistry.FindAndLoadConfigs(new ConfigFilterCriteria
            {
                ManualConfigFiles = settings.Configs,
                Instances = settings.Instances,
                Service = settings.Service
            });

            failureDetected = await ProcessService(settings, configs);
        }
        catch (Exception e)
        {
            if (!await exceptionHandler.HandleException(e))
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
                // todo: Create new NotificationScope here; but how do we collect messages for each config instance we
                // process? Should NotificationService be scoped as well?
                notify.BeginCollecting(config.InstanceName);
                using var scope = configScopeFactory.Start<SyncBasedConfigurationScope>(config);
                await scope.Pipelines.Process(settings);
            }
            catch (Exception e)
            {
                if (!await exceptionHandler.HandleException(e))
                {
                    // This means we didn't handle the exception; rethrow it.
                    throw;
                }

                failureDetected = true;
            }
        }

        return failureDetected;
    }
}
