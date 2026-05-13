using System.Globalization;
using Recyclarr.Pipelines.QualitySize;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizePreviewRenderer(IAnsiConsole console)
    : PreviewRenderer<QualitySizePreviewData>(console)
{
    protected override void RenderData(QualitySizePreviewData data)
    {
        var changedItems = data.Items.Where(x => x.IsDifferent).ToList();

        if (changedItems.Count == 0)
        {
            Console.MarkupLine("[dim]No changes[/]");
            return;
        }

        var limits = data.Limits;
        var table = new Table();
        table.AddColumn("[bold]Quality[/]");
        table.AddColumn("[bold]Min[/]");
        table.AddColumn("[bold]Max[/]");
        table.AddColumn("[bold]Preferred[/]");

        foreach (var item in changedItems)
        {
            table.AddRow(
                $"[dodgerblue1]{Markup.Escape(item.Quality)}[/]",
                item.Min.ToString(CultureInfo.InvariantCulture),
                FormatWithLimit(item.Max, limits.MaxLimit),
                FormatWithLimit(item.Preferred, limits.PreferredLimit)
            );
        }

        Console.Write(table);
    }

    private static string FormatWithLimit(decimal value, decimal limit)
    {
        var formatted = value.ToString(CultureInfo.InvariantCulture);
        return value >= limit ? $"{formatted} (Unlimited)" : formatted;
    }
}
