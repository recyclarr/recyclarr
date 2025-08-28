using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Processors.ErrorHandling;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

[UsedImplicitly]
internal class SyncBasedConfigurationScope(ILifetimeScope scope) : ConfigurationScope(scope)
{
    public ISyncPipeline Pipelines { get; } = scope.Resolve<ISyncPipeline>();
}

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
internal class SyncProcessor(
    IAnsiConsole console,
    ConfigurationRegistry configRegistry,
    ConfigurationScopeFactory configScopeFactory,
    ConsoleExceptionHandler exceptionHandler,
    NotificationService notify
)
{
    public async Task<ExitStatus> Process(ISyncSettings settings, CancellationToken ct)
    {
        notify.BeginWatchEvents();
        var result = await ProcessConfigs(settings, ct);
        await notify.SendNotification(result != ExitStatus.Failed);
        return result;
    }

    private async Task<ExitStatus> ProcessConfigs(ISyncSettings settings, CancellationToken ct)
    {
        bool failureDetected;
        try
        {
            var configs = configRegistry.FindAndLoadConfigs(
                new ConfigFilterCriteria
                {
                    ManualConfigFiles = settings.Configs,
                    Instances = settings.Instances ?? [],
                    Service = settings.Service,
                }
            );

            failureDetected = await ProcessService(settings, configs, ct);
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

    private async Task<bool> ProcessService(
        ISyncSettings settings,
        IEnumerable<IServiceConfiguration> configs,
        CancellationToken ct
    )
    {
        var failureDetected = false;

        foreach (var config in configs)
        {
            try
            {
                using var scope = configScopeFactory.Start<SyncBasedConfigurationScope>(config);
                notify.SetInstanceName(config.InstanceName);

                console.WriteLine(
                    $"""

                    ===========================================
                    Processing {config.ServiceType} Server: [{config.InstanceName}]
                    ===========================================

                    """
                );

                await scope.Pipelines.Execute(settings, ct);
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
