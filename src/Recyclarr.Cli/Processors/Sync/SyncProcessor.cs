using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;
using Recyclarr.Sync;
using Recyclarr.Sync.Events;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Processors.Sync;

[UsedImplicitly]
internal class SyncBasedConfigurationScope(ILifetimeScope scope) : ConfigurationScope(scope)
{
    public PlanBuilder PlanBuilder { get; } = scope.Resolve<PlanBuilder>();
    public ISyncPipeline Pipelines { get; } = scope.Resolve<ISyncPipeline>();
}

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
internal class SyncProcessor(
    ConfigurationRegistry configRegistry,
    ConfigurationScopeFactory configScopeFactory,
    ExceptionHandler exceptionHandler,
    SyncEventOutputStrategy syncEventOutput,
    NotificationService notify,
    ISyncContextSource contextSource,
    SyncEventStorage eventStorage,
    DiagnosticsRenderer diagnosticsRenderer,
    IProgressSource progressSource,
    SyncProgressRenderer progressRenderer
)
{
    public async Task<ExitStatus> Process(ISyncSettings settings, CancellationToken ct)
    {
        var configs = LoadConfigs(settings);
        foreach (var config in configs)
        {
            // All instances are added up front (as opposed to lazily) to support showing
            // the full list of instances in the UI in a pending state before processing begins.
            progressSource.AddInstance(config.InstanceName);
        }

        var result = ExitStatus.Succeeded;
        if (settings.Preview)
        {
            result = await ProcessConfigs(settings, configs, ct);
        }
        else
        {
            await progressRenderer.RenderProgressAsync(
                async () => result = await ProcessConfigs(settings, configs, ct),
                ct
            );
        }

        diagnosticsRenderer.Report();
        await notify.SendNotification(result != ExitStatus.Failed);
        return result;
    }

    private List<IServiceConfiguration> LoadConfigs(ISyncSettings settings)
    {
        return configRegistry
            .FindAndLoadConfigs(
                new ConfigFilterCriteria
                {
                    ManualConfigFiles = settings.Configs,
                    Instances = settings.Instances ?? [],
                    Service = settings.Service,
                }
            )
            .ToList();
    }

    private async Task<ExitStatus> ProcessConfigs(
        ISyncSettings settings,
        IReadOnlyCollection<IServiceConfiguration> configs,
        CancellationToken ct
    )
    {
        bool failureDetected;
        try
        {
            failureDetected = await ProcessService(settings, configs, ct);
        }
        catch (Exception e)
        {
            if (!await exceptionHandler.TryHandleAsync(e, syncEventOutput))
            {
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
            contextSource.SetInstance(config.InstanceName);
            progressSource.SetInstanceStatus(InstanceProgressStatus.Running);

            try
            {
                using var configScope = configScopeFactory.Start<SyncBasedConfigurationScope>(
                    config
                );

                var plan = configScope.PlanBuilder.Build();

                if (eventStorage.HasInstanceErrors(config.InstanceName))
                {
                    progressSource.SetInstanceStatus(InstanceProgressStatus.Failed);
                    failureDetected = true;
                    continue;
                }

                await configScope.Pipelines.Execute(settings, plan, ct);
                progressSource.SetInstanceStatus(InstanceProgressStatus.Succeeded);
            }
            catch (PipelineInterruptException)
            {
                progressSource.SetInstanceStatus(InstanceProgressStatus.Failed);
                failureDetected = true;
            }
            catch (Exception e)
            {
                progressSource.SetInstanceStatus(InstanceProgressStatus.Failed);

                if (!await exceptionHandler.TryHandleAsync(e, syncEventOutput))
                {
                    throw;
                }

                failureDetected = true;
            }
        }

        return failureDetected;
    }
}
