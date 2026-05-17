using System.Globalization;
using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Preview;

internal static class QualitySizePreviewRenderer
{
    public static void Render(IAnsiConsole console, QualitySizeSyncResult result)
    {
        var changedItems = result.Items.Where(x => x.IsDifferent).ToList();

        if (changedItems.Count == 0)
        {
            console.MarkupLine("[dim]No changes[/]");
            return;
        }

        var limits = result.Limits;
        var table = new Table()
            .AddColumn("[bold]Quality[/]")
            .AddColumn("[bold]Min[/]")
            .AddColumn("[bold]Max[/]")
            .AddColumn("[bold]Preferred[/]");

        foreach (var item in changedItems)
        {
            table.AddRow(
                $"[dodgerblue1]{Markup.Escape(item.Quality)}[/]",
                item.Min.ToString(CultureInfo.InvariantCulture),
                FormatWithLimit(item.Max, limits.MaxLimit),
                FormatWithLimit(item.Preferred, limits.PreferredLimit)
            );
        }

        console.Write(table);
    }

    private static string FormatWithLimit(decimal value, decimal limit)
    {
        var formatted = value.ToString(CultureInfo.InvariantCulture);
        return value >= limit ? $"{formatted} (Unlimited)" : formatted;
    }
}
