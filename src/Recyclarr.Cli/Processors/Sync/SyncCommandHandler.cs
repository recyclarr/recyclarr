using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Preview;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;
using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

internal class SyncCommandHandler(
    ILogger log,
    IAnsiConsole console,
    ISyncOrchestrator orchestrator,
    ConfigPipelineFactory configPipelineFactory,
    SyncProgressRenderer progressRenderer,
    DiagnosticsRenderer diagnosticsRenderer,
    PreviewRenderer previewRenderer,
    INotificationService notify
)
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task<ExitStatus> RunAsync(ISyncSettings settings, CancellationToken ct)
    {
        var configs = LoadConfigs(settings);
        var instanceNames = configs.Select(c => c.InstanceName).ToList();

        var result = ExitStatus.Succeeded;
        if (settings.Preview)
        {
            var jobResult = await orchestrator.RunAsync(configs, settings, ct);
            result = jobResult.Status;
            previewRenderer.Render(jobResult.JobId, instanceNames);
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

        try
        {
            await notify.SendNotification();
        }
        catch (Exception e)
        {
            console.MarkupLine(
                $"[yellow]Warning:[/] Failed to send notification: {e.Message.EscapeMarkup()}"
            );
            log.Warning(e, "Failed to send notification");
        }

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
