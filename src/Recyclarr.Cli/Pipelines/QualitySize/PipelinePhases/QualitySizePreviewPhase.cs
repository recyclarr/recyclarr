using System.Globalization;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizePreviewPhase(IAnsiConsole console)
    : PreviewPipelinePhase<QualitySizePipelineContext>
{
    protected override void RenderPreview(QualitySizePipelineContext context)
    {
        var limits = context.Limits;
        var table = new Table();

        table.Title("Quality Sizes [red](Preview)[/]");
        table.AddColumn("[bold]Quality[/]");
        table.AddColumn("[bold]Min[/]");
        table.AddColumn("[bold]Max[/]");
        table.AddColumn("[bold]Preferred[/]");

        foreach (var item in context.TransactionOutput)
        {
            var style = item.IsDifferent ? "bold " : "dim ";
            table.AddRow(
                $"[{style}dodgerblue1]{item.Quality}[/]",
                $"[{style}default]{item.Min.ToString(CultureInfo.InvariantCulture)}[/]",
                $"[{style}default]{FormatWithLimit(item.Max, limits.MaxLimit)}[/]",
                $"[{style}default]{FormatWithLimit(item.Preferred, limits.PreferredLimit)}[/]"
            );
        }

        table.Caption("[grey]Bold items will be updated[/]");

        console.WriteLine();
        console.Write(table);
    }

    private static string FormatWithLimit(decimal value, decimal limit)
    {
        var formatted = value.ToString(CultureInfo.InvariantCulture);
        return value >= limit ? $"{formatted} (Unlimited)" : formatted;
    }
}
