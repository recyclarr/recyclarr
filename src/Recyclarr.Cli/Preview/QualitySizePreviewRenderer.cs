using System.Globalization;
using Recyclarr.Client.V1;
using Spectre.Console;

namespace Recyclarr.Cli.Preview;

internal static class QualitySizePreviewRenderer
{
    public static void Render(IAnsiConsole console, QualitySizesResultResponse result)
    {
        // The wire only carries sizes that differ, so no further filtering is needed here.
        if (result.Items.Count == 0)
        {
            console.MarkupLine("[dim]No changes[/]");
            return;
        }

        var table = new Table()
            .AddColumn("[bold]Quality[/]")
            .AddColumn("[bold]Min[/]")
            .AddColumn("[bold]Max[/]")
            .AddColumn("[bold]Preferred[/]");

        foreach (var item in result.Items)
        {
            table.AddRow(
                $"[dodgerblue1]{Markup.Escape(item.Quality)}[/]",
                item.Min.ToString(CultureInfo.InvariantCulture),
                FormatWithLimit(item.Max, result.MaxLimit),
                FormatWithLimit(item.Preferred, result.PreferredLimit)
            );
        }

        console.Write(table);
    }

    private static string FormatWithLimit(double value, double limit)
    {
        var formatted = value.ToString(CultureInfo.InvariantCulture);
        return value >= limit ? $"{formatted} (Unlimited)" : formatted;
    }
}
