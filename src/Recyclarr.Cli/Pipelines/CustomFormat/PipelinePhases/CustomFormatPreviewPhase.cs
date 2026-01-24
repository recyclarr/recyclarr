using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatPreviewPhase(IAnsiConsole console, ISyncContextSource contextSource)
    : PreviewPipelinePhase<CustomFormatPipelineContext>(console, contextSource)
{
    protected override void RenderPreview(CustomFormatPipelineContext context)
    {
        RenderTitle(context);

        var transactions = context.TransactionOutput;

        if (transactions.TotalCustomFormatChanges == 0)
        {
            Console.MarkupLine("[dim]No changes[/]");
            return;
        }

        PlannedCustomFormat? GetPlanned(string trashId) => context.Plan.GetCustomFormat(trashId);

        string GetSourceDisplay(PlannedCustomFormat? planned)
        {
            if (planned is null)
            {
                return "(from custom_formats)";
            }

            return planned.Source switch
            {
                CfSource.FlatConfig => "(from custom_formats)",
                CfSource.ProfileFormatItems => $"(from profile: {GetProfileNames(planned)})",
                CfSource.CfGroupImplicit =>
                    $"(from group: {planned.GroupName} [implicit via: {GetProfileNames(planned)}])",
                CfSource.CfGroupExplicit => $"(from group: {planned.GroupName} [explicit])",
                _ => planned.GroupName ?? "(from custom_formats)",
            };

            static string GetProfileNames(PlannedCustomFormat planned) =>
                string.Join(", ", planned.AssignScoresTo.Select(a => a.Name));
        }

        string GetInclusionDisplay(PlannedCustomFormat? planned) =>
            planned?.InclusionReason switch
            {
                CfInclusionReason.Required => "required",
                CfInclusionReason.Default => "default",
                CfInclusionReason.Selected => "selected",
                _ => "",
            };

        // Build tuples for all changes
        var allChanges = transactions
            .NewCustomFormats.Select(cf =>
            {
                var planned = GetPlanned(cf.TrashId);
                return (
                    Source: GetSourceDisplay(planned),
                    Action: "Create",
                    Color: "green",
                    cf.Name,
                    cf.TrashId,
                    Inclusion: GetInclusionDisplay(planned)
                );
            })
            .Concat(
                transactions.UpdatedCustomFormats.Select(cf =>
                {
                    var planned = GetPlanned(cf.TrashId);
                    return (
                        Source: GetSourceDisplay(planned),
                        Action: "Update",
                        Color: "yellow",
                        cf.Name,
                        cf.TrashId,
                        Inclusion: GetInclusionDisplay(planned)
                    );
                })
            )
            .Concat(
                transactions.DeletedCustomFormats.Select(m =>
                {
                    var planned = GetPlanned(m.TrashId);
                    return (
                        Source: GetSourceDisplay(planned),
                        Action: "Delete",
                        Color: "red",
                        m.Name,
                        m.TrashId,
                        Inclusion: GetInclusionDisplay(planned)
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

        Console.Write(tree);

        var unchanged = transactions.UnchangedCustomFormats.Count;
        if (unchanged > 0)
        {
            Console.MarkupLine($"[dim]Unchanged: {unchanged}[/]");
        }
    }
}
