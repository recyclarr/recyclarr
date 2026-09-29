using Recyclarr.Client.V1;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Processors.Sync.Results;

/// <summary>
/// Builds the final status table shown after a sync ends: one row per instance with a cell per
/// pipeline. A succeeded cell shows how many items changed (or a check mark when nothing did).
/// Instances without results (never run or interrupted) keep their last progress status.
/// </summary>
internal static class SyncResultsTable
{
    public static IRenderable Build(
        IEnumerable<InstanceSnapshotResponse> progress,
        IReadOnlyDictionary<string, InstanceResults> results
    )
    {
        var table = new Table().Border(TableBorder.None);
        table.AddColumn(new TableColumn(""));
        table.AddColumn(new TableColumn("").PadRight(2));
        string[] pipelineHeaders =
        [
            "Custom\nFormats",
            "Quality\nProfiles",
            "Quality\nSizes",
            "Media\nNaming",
            "Media\nMgmt",
        ];

        foreach (var header in pipelineHeaders)
        {
            table.AddColumn(new TableColumn($"[blue]{header}[/]").RightAligned());
        }

        var progressByName = progress.ToDictionary(x => x.Name, StringComparer.Ordinal);
        var names = progressByName.Keys.Union(results.Keys, StringComparer.Ordinal);

        foreach (var instanceName in names)
        {
            var name = new Markup($"[white]{instanceName.EscapeMarkup()}[/]");
            if (!results.TryGetValue(instanceName, out var result))
            {
                table.AddRow([
                    new Markup(StatusMarkup.Of(progressByName[instanceName].Status)),
                    name,
                    .. NotRun(pipelineHeaders),
                ]);
                continue;
            }

            table.AddRow(
                new Markup(StatusMarkup.Of(result.Status)),
                name,
                Cell(result.CustomFormats?.Status, result.CustomFormatChanges),
                Cell(result.QualityProfiles?.Status, result.QualityProfileChanges),
                Cell(result.QualitySizes?.Status, result.QualitySizeChanges),
                Cell(result.Naming?.Status, result.NamingChanges),
                Cell(result.MediaManagement?.Status, result.MediaManagementChanges)
            );
        }

        return table;
    }

    private static IEnumerable<Markup> NotRun(IEnumerable<string> pipelines) =>
        pipelines.Select(_ => new Markup(StatusMarkup.NotRun));

    private static Markup Cell(PipelineStatus? status, int changes) =>
        new(
            status switch
            {
                PipelineStatus.Succeeded when changes > 0 => $"[green]{changes}[/]",
                PipelineStatus.Succeeded => StatusMarkup.Ok,
                PipelineStatus.Partial => StatusMarkup.Partial,
                PipelineStatus.Failed => StatusMarkup.Failed,
                _ => StatusMarkup.NotRun,
            }
        );
}
