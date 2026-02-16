using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Processors.Sync;

internal class SyncProcessor(
    ConfigurationRegistry configRegistry,
    LifetimeScopeFactory scopeFactory,
    NotificationService notify,
    DiagnosticsRenderer diagnosticsRenderer,
    IProgressSource progressSource,
    ISyncRunPublisher runPublisher,
    SyncProgressRenderer progressRenderer
)
{
    public async Task<ExitStatus> Process(ISyncSettings settings, CancellationToken ct)
    {
        var configs = LoadConfigs(settings);
        var instanceNames = configs.Select(c => c.InstanceName).ToList();

        foreach (var config in configs)
        {
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
            var instancePublisher = new InstancePublisher(config.InstanceName, runPublisher);

            progressSource.SetInstanceStatus(config.InstanceName, InstanceProgressStatus.Running);
            instancePublisher.SetStatus(InstanceProgressStatus.Running);

            using var instanceScope = scopeFactory.Start<InstanceScope>(
                "instance",
                c => c.RegisterInstance(config).AsSelf().As<IServiceConfiguration>()
            );
            var result = await instanceScope.InstanceProcessor.Process(
                settings,
                instancePublisher,
                ct
            );

            if (result == InstanceSyncResult.Failed)
            {
                progressSource.SetInstanceStatus(
                    config.InstanceName,
                    InstanceProgressStatus.Failed
                );
                instancePublisher.SetStatus(InstanceProgressStatus.Failed);
                failureDetected = true;
            }
            else
            {
                progressSource.SetInstanceStatus(
                    config.InstanceName,
                    InstanceProgressStatus.Succeeded
                );
                instancePublisher.SetStatus(InstanceProgressStatus.Succeeded);
            }
        }

        return failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
    }
}
