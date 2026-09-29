using Recyclarr.Client.V1;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync.Results;

/// <summary>
/// Presents a finished sync job: the final status table, preview change sections (preview runs
/// only), and the diagnostics panel on the console, plus the same diagnostics and a per-instance
/// change summary in the log. Console and log are separate channels; <c>--log</c> silences the
/// console, so the log alone must carry every actionable message.
/// </summary>
internal class SyncResultsPresenter(IAnsiConsole console, ILogger log, PreviewRenderer preview)
{
    public void Present(
        SyncJobResultsResponse results,
        IEnumerable<InstanceSnapshotResponse> progress,
        bool isPreview
    )
    {
        var instances = results.Instances.Select(InstanceResults.From).ToList();

        console.Write(
            SyncResultsTable.Build(
                progress,
                instances.ToDictionary(x => x.Name, StringComparer.Ordinal)
            )
        );

        if (isPreview)
        {
            instances.ForEach(preview.Render);
        }

        var diagnostics = SyncDiagnostics.Build(results.Fault, instances);
        if (DiagnosticsPanel.Build(diagnostics) is { } panel)
        {
            console.WriteLine();
            console.Write(panel);
        }

        console.WriteLine();

        foreach (var instance in instances)
        {
            log.Information(
                "Instance {Instance:l} finished with status {Status}; changes: {Changes:l}",
                instance.Name,
                instance.Status,
                ChangeSummary(instance)
            );
        }

        foreach (var diagnostic in diagnostics)
        {
            var message = diagnostic.Instance is null
                ? diagnostic.Message
                : $"[{diagnostic.Instance}] {diagnostic.Message}";

            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                log.Error("{Message:l}", message);
            }
            else
            {
                log.Warning("{Message:l}", message);
            }
        }
    }

    private static string ChangeSummary(InstanceResults instance)
    {
        (string Name, int Count)[] counts =
        [
            (PipelineNames.CustomFormats, instance.CustomFormatChanges),
            (PipelineNames.QualityProfiles, instance.QualityProfileChanges),
            (PipelineNames.QualitySizes, instance.QualitySizeChanges),
            (PipelineNames.MediaNaming, instance.NamingChanges),
            (PipelineNames.MediaManagement, instance.MediaManagementChanges),
        ];

        var changed = counts.Where(x => x.Count > 0).Select(x => $"{x.Name} {x.Count}").ToList();
        return changed.Count == 0 ? "none" : string.Join(", ", changed);
    }
}
