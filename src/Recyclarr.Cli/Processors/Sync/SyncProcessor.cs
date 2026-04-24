using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;

namespace Recyclarr.Cli.Processors.Sync;

internal class SyncProcessor(
    ConfigPipelineFactory configPipelineFactory,
    InstanceScopeFactory instanceScopeFactory,
    INotificationService notify,
    DiagnosticsRenderer diagnosticsRenderer,
    DiagnosticsLogger diagnosticsLogger,
    SyncProgressRenderer progressRenderer
)
{
    public async Task<ExitStatus> Process(ISyncSettings settings, CancellationToken ct)
    {
        // Injected to activate its diagnostic subscription; no callable API
        _ = diagnosticsLogger;

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
        var pipeline =
            settings.Configs.Count > 0
                ? configPipelineFactory.FromPaths(settings.Configs)
                : configPipelineFactory.FromDefaultPaths();

        return pipeline
            .FilterByInstance(settings.Instances)
            .FilterByService(settings.Service)
            .GetConfigs()
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
            using var instanceScope = instanceScopeFactory.Start<InstanceSyncProcessor>(config);

            var result = await instanceScope.Entry.Process(settings, ct);
            if (result == InstanceSyncResult.Failed)
            {
                failureDetected = true;
            }
        }

        return failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
    }
}
