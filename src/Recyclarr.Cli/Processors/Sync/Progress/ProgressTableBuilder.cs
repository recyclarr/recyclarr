using Recyclarr.Client.V1;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Processors.Sync.Progress;

// Progress is instance-level only: the polled job carries each instance's status, not pipeline
// detail (ADR-018).
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

    public static IRenderable BuildTable(
        IEnumerable<InstanceSnapshotResponse> instances,
        string spinnerFrame
    )
    {
        var table = new Table().Border(TableBorder.None).HideHeaders();
        table.AddColumn(new TableColumn("").Width(1));
        table.AddColumn(new TableColumn(""));

        foreach (var instance in instances)
        {
            var isActive = instance.Status == InstanceProgressStatusResponse.Running;
            var bold = isActive ? " bold" : "";
            table.AddRow(
                new Markup(StatusIcon(instance.Status, spinnerFrame, bold)),
                new Markup($"[white{bold}]{instance.Name.EscapeMarkup()}[/]")
            );
        }

        return table;
    }

    private static string StatusIcon(
        InstanceProgressStatusResponse status,
        string spinnerFrame,
        string bold
    ) =>
        status switch
        {
            InstanceProgressStatusResponse.Pending => $"[grey{bold}]{spinnerFrame}[/]",
            InstanceProgressStatusResponse.Running => $"[blue{bold}]{spinnerFrame}[/]",
            InstanceProgressStatusResponse.Succeeded => "[green]✓[/]",
            InstanceProgressStatusResponse.Partial => "[yellow]~[/]",
            InstanceProgressStatusResponse.Failed => "[red]✗[/]",
            InstanceProgressStatusResponse.Interrupted or InstanceProgressStatusResponse.NotRun =>
                "[grey]--[/]",
            _ => " ",
        };
}
