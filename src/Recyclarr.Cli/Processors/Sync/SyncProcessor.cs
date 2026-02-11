using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Processors.Sync;

[UsedImplicitly]
internal class SyncBasedConfigurationScope(ILifetimeScope scope) : ConfigurationScope(scope)
{
    public InstanceSyncProcessor InstanceProcessor { get; } =
        scope.Resolve<InstanceSyncProcessor>();
}

internal class SyncProcessor(
    ConfigurationRegistry configRegistry,
    ConfigurationScopeFactory configScopeFactory,
    NotificationService notify,
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

        var snapshot = new ProgressSnapshot([]);
        using var subscription = progressSource.Observable.Subscribe(s => snapshot = s);

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
        await notify.SendNotification(result != ExitStatus.Failed, snapshot);
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
        var failureDetected = false;

        foreach (var config in configs)
        {
            progressSource.SetInstanceStatus(InstanceProgressStatus.Running);

            using var configScope = configScopeFactory.Start<SyncBasedConfigurationScope>(config);
            var result = await configScope.InstanceProcessor.Process(settings, ct);

            if (result == InstanceSyncResult.Failed)
            {
                progressSource.SetInstanceStatus(InstanceProgressStatus.Failed);
                failureDetected = true;
            }
            else
            {
                progressSource.SetInstanceStatus(InstanceProgressStatus.Succeeded);
            }
        }

        return failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
    }
}
