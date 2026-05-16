using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Config.Models;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Processors.Sync;

internal class SyncCommandHandler(
    ISyncOrchestrator orchestrator,
    ConfigPipelineFactory configPipelineFactory,
    SyncProgressRenderer progressRenderer,
    DiagnosticsRenderer diagnosticsRenderer
)
{
    public async Task<ExitStatus> RunAsync(ISyncSettings settings, CancellationToken ct)
    {
        var configs = LoadConfigs(settings);
        var instanceNames = configs.Select(c => c.InstanceName).ToList();

        var result = ExitStatus.Succeeded;
        if (settings.Preview)
        {
            result = (await orchestrator.RunAsync(configs, settings, ct)).Status;
        }
        else
        {
            await progressRenderer.RenderProgressAsync(
                instanceNames,
                async () => result = (await orchestrator.RunAsync(configs, settings, ct)).Status,
                ct
            );
        }

        diagnosticsRenderer.Report();
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
}
