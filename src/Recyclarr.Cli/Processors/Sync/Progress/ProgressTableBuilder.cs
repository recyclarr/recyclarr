using System.Globalization;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Processors.Sync.Progress;

internal class ProgressTableBuilder
{
    private static readonly string[] SpinnerFrames =
    [
        "⠋",
        "⠙",
        "⠹",
        "⠸",
        "⠼",
        "⠴",
        "⠦",
        "⠧",
        "⠇",
        "⠏",
    ];

    private int _frameIndex;

    public string GetNextSpinnerFrame()
    {
        var frame = SpinnerFrames[_frameIndex];
        _frameIndex = (_frameIndex + 1) % SpinnerFrames.Length;
        return frame;
    }

    public static IRenderable BuildTable(ProgressSnapshot snapshot, string spinnerFrame)
    {
        var table = new Table().Border(TableBorder.None);

        table.AddColumn(new TableColumn("").Width(1));
        table.AddColumn(new TableColumn("").PadRight(2));
        table.AddColumn(new TableColumn(new Markup("[blue]Custom\nFormats[/]")).RightAligned());
        table.AddColumn(new TableColumn(new Markup("[blue]Quality\nProfiles[/]")).RightAligned());
        table.AddColumn(new TableColumn(new Markup("[blue]Quality\nSizes[/]")).RightAligned());
        table.AddColumn(new TableColumn(new Markup("[blue]Media\nNaming[/]")).RightAligned());
        table.AddColumn(new TableColumn(new Markup("[blue]Media\nMgmt[/]")).RightAligned());

        foreach (var instance in snapshot.Instances)
        {
            AddInstanceRow(table, instance, spinnerFrame);
        }

        return table;
    }

    private static void AddInstanceRow(Table table, InstanceSnapshot instance, string spinnerFrame)
    {
        var isActive = instance.Status == InstanceProgressStatus.Running;
        var boldStyle = isActive ? " bold" : "";

        var statusIcon = instance.Status switch
        {
            InstanceProgressStatus.Pending => $"[grey{boldStyle}]{spinnerFrame}[/]",
            InstanceProgressStatus.Running => $"[blue{boldStyle}]{spinnerFrame}[/]",
            InstanceProgressStatus.Succeeded => "[green]✓[/]",
            InstanceProgressStatus.Failed => "[red]✗[/]",
            _ => " ",
        };

        var nameStyle = isActive ? "white bold" : "white";
        var instanceFailed = instance.Status == InstanceProgressStatus.Failed;

        table.AddRow(
            new Markup(statusIcon),
            new Markup($"[{nameStyle}]{instance.Name.EscapeMarkup()}[/]"),
            FormatPipeline(PipelineType.CustomFormat),
            FormatPipeline(PipelineType.QualityProfile),
            FormatPipeline(PipelineType.QualitySize),
            FormatPipeline(PipelineType.MediaNaming),
            FormatPipeline(PipelineType.MediaManagement)
        );
        return;

        Markup FormatPipeline(PipelineType type)
        {
            var result = instance.Pipelines.TryGetValue(type, out var pipeline)
                ? pipeline
                : (PipelineSnapshot?)null;
            return new Markup(FormatPipelineStatus(result, spinnerFrame, isActive, instanceFailed));
        }
    }

    private static string FormatPipelineStatus(
        PipelineSnapshot? result,
        string spinnerFrame,
        bool isActive,
        bool instanceFailed
    )
    {
        const int width = 3;
        string value;
        string color;

        if (result is null)
        {
            value = instanceFailed ? "--" : spinnerFrame;
            color = "grey";
        }
        else
        {
            (value, color) = result.Value.Status switch
            {
                PipelineProgressStatus.Pending => (spinnerFrame, "grey"),
                PipelineProgressStatus.Running => (spinnerFrame, "yellow"),
                PipelineProgressStatus.Succeeded => (
                    result.Value.Count > 0
                        ? result.Value.Count.Value.ToString(CultureInfo.InvariantCulture)
                        : "✓",
                    "green"
                ),
                PipelineProgressStatus.Failed => ("✗", "red"),
                PipelineProgressStatus.Skipped => ("--", "grey"),
                _ => ("?", "white"),
            };
        }

        var boldStyle = isActive ? " bold" : "";
        return $"[{color}{boldStyle}]{value, width}[/]";
    }
}
