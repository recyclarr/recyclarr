using Recyclarr.Client.V1;
using Spectre.Console;

namespace Recyclarr.Cli.Preview;

internal static class CustomFormatPreviewRenderer
{
    public static void Render(IAnsiConsole console, CustomFormatsResultResponse result)
    {
        if (result.Changes.Count == 0)
        {
            console.MarkupLine("[dim]No changes[/]");
            return;
        }

        // Group the action-tagged changes by their source label, keeping flat config first.
        var allChanges = result
            .Changes.Select(x =>
                (
                    Source: GetSourceDisplay(x.Source),
                    x.Action,
                    Color: GetActionColor(x.Action),
                    x.Name,
                    x.TrashId,
                    Inclusion: GetInclusionDisplay(x.InclusionReason)
                )
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

        if (result.UnchangedCount > 0)
        {
            console.MarkupLine($"[dim]Unchanged: {result.UnchangedCount}[/]");
        }
    }

    private static string GetActionColor(string action) =>
        action switch
        {
            "Create" => "green",
            "Update" => "yellow",
            "Delete" => "red",
            _ => "default",
        };

    private static string GetSourceDisplay(CustomFormatSourceResponse? source)
    {
        if (source is null)
        {
            return "(from custom_formats)";
        }

        var profileNames = string.Join(", ", source.ProfileNames ?? []);
        return source.Source switch
        {
            "ProfileFormatItems" => $"(from profile: {profileNames})",
            "CfGroupImplicit" => $"(from group: {source.GroupName} [implicit via: {profileNames}])",
            "CfGroupExplicit" => $"(from group: {source.GroupName} [explicit])",
            _ => "(from custom_formats)",
        };
    }

    // The wire carries the reason as its domain name; the table renders it lower case.
    private static string GetInclusionDisplay(string? inclusionReason) =>
        inclusionReason switch
        {
            "Required" => "required",
            "Default" => "default",
            "Selected" => "selected",
            _ => "",
        };
}
