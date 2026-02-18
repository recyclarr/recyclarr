using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;

namespace Recyclarr.Cli.Processors.Sync;

internal class SyncProcessor(
    ConfigurationRegistry configRegistry,
    LifetimeScopeFactory scopeFactory,
    NotificationService notify,
    DiagnosticsRenderer diagnosticsRenderer,
    SyncProgressRenderer progressRenderer
)
{
    public async Task<ExitStatus> Process(ISyncSettings settings, CancellationToken ct)
    {
        var configs = LoadConfigs(settings);
        var instanceNames = configs.Select(c => c.InstanceName).ToList();

        var result = ExitStatus.Succeeded;
        if (settings.Preview)
        {
            result = await ProcessConfigs(settings, configs, ct);
        }
        else
        {
            await progressRenderer.RenderProgressAsync(
                instanceNames,
                async () => result = await ProcessConfigs(settings, configs, ct),
                ct
            );
        }

        diagnosticsRenderer.Report();
        await notify.SendNotification();
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
            using var instanceScope = scopeFactory.Start<InstanceScope>(
                "instance",
                c =>
                {
                    c.RegisterInstance(config).AsSelf().As<IServiceConfiguration>();
                }
            );

            var result = await instanceScope.InstanceProcessor.Process(settings, ct);
            if (result == InstanceSyncResult.Failed)
            {
                failureDetected = true;
            }
        }

        return failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
    }
}
