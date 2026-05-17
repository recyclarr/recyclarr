using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Preview;

internal static class CustomFormatPreviewRenderer
{
    public static void Render(IAnsiConsole console, CustomFormatSyncResult result)
    {
        var transactions = result.Transactions;

        if (transactions.TotalCustomFormatChanges == 0)
        {
            console.MarkupLine("[dim]No changes[/]");
            return;
        }

        // Build tuples for all changes, grouped by source display label
        var allChanges = transactions
            .NewCustomFormats.Select(cf =>
            {
                var info = result.SourceInfo.GetValueOrDefault(cf.TrashId);
                return (
                    Source: GetSourceDisplay(info),
                    Action: "Create",
                    Color: "green",
                    cf.Name,
                    cf.TrashId,
                    Inclusion: GetInclusionDisplay(info)
                );
            })
            .Concat(
                transactions.UpdatedCustomFormats.Select(cf =>
                {
                    var info = result.SourceInfo.GetValueOrDefault(cf.TrashId);
                    return (
                        Source: GetSourceDisplay(info),
                        Action: "Update",
                        Color: "yellow",
                        cf.Name,
                        cf.TrashId,
                        Inclusion: GetInclusionDisplay(info)
                    );
                })
            )
            .Concat(
                transactions.DeletedCustomFormats.Select(m =>
                {
                    var info = result.SourceInfo.GetValueOrDefault(m.TrashId);
                    return (
                        Source: GetSourceDisplay(info),
                        Action: "Delete",
                        Color: "red",
                        m.Name,
                        m.TrashId,
                        Inclusion: GetInclusionDisplay(info)
                    );
                })
            )
            .GroupBy(x => x.Source)
            .OrderBy(g => g.Key == "(from custom_formats)" ? 0 : 1)
            .ThenBy(g => g.Key);

        var tree = new Tree("[bold]Changes[/]");

        foreach (var sourceGroup in allChanges)
        {
            // Only show Inclusion column for CF groups (implicit/explicit)
            var isGroup =
                sourceGroup.Key.Contains("[implicit]", StringComparison.Ordinal)
                || sourceGroup.Key.Contains("[explicit]", StringComparison.Ordinal);

            var table = new Table()
                .AddColumn("[bold]Action[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Trash ID[/]");

            if (isGroup)
            {
                table.AddColumn("[bold]Inclusion[/]");
            }

            foreach (var (_, action, color, name, trashId, inclusion) in sourceGroup)
            {
                if (isGroup)
                {
                    table.AddRow(
                        $"[{color}]{action}[/]",
                        name.EscapeMarkup(),
                        $"[dim]{trashId}[/]",
                        $"[dim]{inclusion}[/]"
                    );
                }
                else
                {
                    table.AddRow(
                        $"[{color}]{action}[/]",
                        name.EscapeMarkup(),
                        $"[dim]{trashId}[/]"
                    );
                }
            }

            tree.AddNode(new Rows(new Markup($"[dim]{sourceGroup.Key.EscapeMarkup()}[/]"), table));
        }

        console.Write(tree);

        var unchanged = transactions.UnchangedCustomFormats.Count;
        if (unchanged > 0)
        {
            console.MarkupLine($"[dim]Unchanged: {unchanged}[/]");
        }
    }

    private static string GetSourceDisplay(CustomFormatSourceInfo? info) =>
        info is null
            ? "(from custom_formats)"
            : info.Source switch
            {
                CfSource.FlatConfig => "(from custom_formats)",
                CfSource.ProfileFormatItems =>
                    $"(from profile: {string.Join(", ", info.ProfileNames)})",
                CfSource.CfGroupImplicit =>
                    $"(from group: {info.GroupName} [implicit via: {string.Join(", ", info.ProfileNames)}])",
                CfSource.CfGroupExplicit => $"(from group: {info.GroupName} [explicit])",
                _ => "(from custom_formats)",
            };

    private static string GetInclusionDisplay(CustomFormatSourceInfo? info) =>
        info?.InclusionReason switch
        {
            CfInclusionReason.Required => "required",
            CfInclusionReason.Default => "default",
            CfInclusionReason.Selected => "selected",
            _ => "",
        };
}
