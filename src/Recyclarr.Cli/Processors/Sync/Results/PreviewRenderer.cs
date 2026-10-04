using Recyclarr.Client.V1;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Processors.Sync.Results;

/// <summary>
/// Renders what a preview sync would change, one section per pipeline of each instance. Values
/// show as "current -> desired" wherever the results carry both.
/// </summary>
internal class PreviewRenderer(IAnsiConsole console)
{
    public void Render(InstanceResults instance)
    {
        console.WriteLine();
        console.Write(new Rule($"[bold]{instance.Name.EscapeMarkup()}[/]").LeftJustified());

        if (instance.CustomFormats is { } cf)
        {
            Section(instance, "Custom Format", cf.BlockedBy, CustomFormats(cf));
        }

        if (instance.QualityProfiles is { } qp)
        {
            Section(instance, "Quality Profile", qp.BlockedBy, QualityProfiles(qp));
        }

        if (instance.QualitySizes is { } qs)
        {
            Section(instance, "Quality Definition", qs.BlockedBy, QualitySizes(qs));
        }

        if (instance.Naming is { } naming)
        {
            Section(instance, naming.Title, naming.BlockedBy, Fields(naming.Changes));
        }

        if (instance.MediaManagement is { } mm)
        {
            var changes = mm
                .Updates.Select(x =>
                    FieldChange.From("Download Propers and Repacks", x.PropersAndRepacks)
                )
                .OfType<FieldChange>()
                .ToList();

            Section(instance, "Media Management", mm.BlockedBy, Fields(changes));
        }
    }

    private void Section(
        InstanceResults instance,
        string title,
        BlockingPipeline? blockedBy,
        IRenderable? changes
    )
    {
        console.WriteLine();
        console.MarkupLine(
            $"── [bold]{title}[/] [red](Preview)[/] [dim][[{instance.Name.EscapeMarkup()}]][/] ──"
        );

        if (blockedBy is { } blocker)
        {
            console.MarkupLine($"[dim]Not run: blocked by {PipelineNames.Of(blocker)}[/]");
            return;
        }

        if (changes is null)
        {
            console.MarkupLine("[dim]No changes[/]");
            return;
        }

        console.Write(changes);
    }

    private static Tree? CustomFormats(CustomFormatPipelineResponse pipeline)
    {
        const string flatSource = "(from custom_formats)";

        var groups = pipeline
            .Creates.Select(x => new CfRow("[green]Create[/]", x.Identity, x.SelectionProvenance))
            .Concat(
                pipeline.Updates.Select(x => new CfRow(
                    "[yellow]Update[/]",
                    x.Identity,
                    x.SelectionProvenance
                ))
            )
            .Concat(pipeline.Deletes.Select(x => new CfRow("[red]Delete[/]", x, null)))
            .GroupBy(x => x.Provenance is null ? flatSource : Source(x.Provenance))
            .OrderBy(x => x.Key == flatSource ? 0 : 1)
            .ThenBy(x => x.Key, StringComparer.Ordinal)
            .ToList();

        if (groups.Count == 0)
        {
            return null;
        }

        var tree = new Tree("[bold]Changes[/]");
        foreach (var group in groups)
        {
            var isGroup = group.Any(x => x.Provenance?.GroupName is not null);
            var table = new Table()
                .AddColumn("[bold]Action[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Trash ID[/]");

            if (isGroup)
            {
                table.AddColumn("[bold]Inclusion[/]");
            }

            foreach (var (action, identity, provenance) in group)
            {
                string[] cells =
                [
                    action,
                    identity.Name.EscapeMarkup(),
                    $"[dim]{identity.TrashId.EscapeMarkup()}[/]",
                ];

                table.AddRow(isGroup ? [.. cells, $"[dim]{Inclusion(provenance)}[/]"] : cells);
            }

            tree.AddNode(new Rows(new Markup($"[dim]{group.Key.EscapeMarkup()}[/]"), table));
        }

        return tree;

        static string Source(CustomFormatSelectionResponse selection)
        {
            var profiles = string.Join(", ", selection.ProfileNames);
            return selection.Source switch
            {
                CustomFormatSource.ProfileFormatItems => $"(from profile: {profiles})",
                CustomFormatSource.CfGroupImplicit =>
                    $"(from group: {selection.GroupName} [implicit via: {profiles}])",
                CustomFormatSource.CfGroupExplicit =>
                    $"(from group: {selection.GroupName} [explicit])",
                _ => flatSource,
            };
        }

        static string Inclusion(CustomFormatSelectionResponse? selection) =>
            selection?.InclusionReason switch
            {
                CustomFormatInclusionReason.Required => "required",
                CustomFormatInclusionReason.Default => "default",
                CustomFormatInclusionReason.Selected => "selected",
                _ => "",
            };
    }

    private static Rows? QualityProfiles(QualityProfileResults pipeline)
    {
        if (pipeline.Changes.Count == 0)
        {
            return null;
        }

        var rows = new List<IRenderable>();

        foreach (var change in pipeline.Changes)
        {
            var tree = ProfileTree(change.Identity, change.Reason);
            AddFields(tree, change.Fields);
            AddQualities(tree, change.CurrentQualities, change.DesiredQualities);
            AddScores(tree, change.Scores);
            rows.Add(tree);
        }

        return new Rows(rows);

        static Tree ProfileTree(QualityProfileIdentityResponse identity, string reason) =>
            new($"[yellow]{identity.Name.EscapeMarkup()}[/] (Change Reason: [green]{reason}[/])");

        static void AddFields(Tree tree, IReadOnlyList<FieldChange> fields)
        {
            if (fields.Count == 0)
            {
                return;
            }

            tree.AddNode(new Rows(new Markup("[b]Profile Updates[/]"), FieldTable(fields)));
        }

        static void AddQualities(
            Tree tree,
            IReadOnlyCollection<QualityProfileLayoutResponse> current,
            IReadOnlyCollection<QualityProfileLayoutResponse> desired
        )
        {
            if (desired.Count == 0)
            {
                return;
            }

            var columns = new Columns(
                Layout("Current", current),
                Layout("New", desired)
            ).Collapse();

            tree.AddNode(new Rows(new Markup("[b]Quality Updates[/]"), columns));
        }

        static Panel Layout(string header, IReadOnlyCollection<QualityProfileLayoutResponse> items)
        {
            var tree = new Tree("");
            foreach (var item in items)
            {
                var mark = item.Allowed ? "[green]✓[/]" : "[red]✗[/]";
                var node = tree.AddNode($"{mark} {item.Name.EscapeMarkup()}");
                foreach (var member in item.Qualities ?? [])
                {
                    node.AddNode(member.EscapeMarkup());
                }
            }

            return new Panel(items.Count == 0 ? new Markup("[dim]none[/]") : tree)
                .Header($"[bold][underline]{header}[/][/]")
                .NoBorder();
        }

        static void AddScores(Tree tree, IReadOnlyList<ScoreChange> scores)
        {
            if (scores.Count == 0)
            {
                return;
            }

            var table = new Table()
                .AddColumn("[bold]Custom Format[/]")
                .AddColumn("[bold]Current[/]")
                .AddColumn("[bold]New[/]")
                .AddColumn("[bold]Reason[/]");

            foreach (var score in scores)
            {
                table.AddRow(score.Name.EscapeMarkup(), score.Current, score.Desired, score.Reason);
            }

            tree.AddNode(new Rows(new Markup("[b]Score Updates[/]"), table));
        }
    }

    private static Table? QualitySizes(QualitySizePipelineResponse pipeline)
    {
        if (pipeline.Updates.Count == 0)
        {
            return null;
        }

        var table = new Table()
            .AddColumn("[bold]Quality[/]")
            .AddColumn("[bold]Min[/]")
            .AddColumn("[bold]Max[/]")
            .AddColumn("[bold]Preferred[/]");

        foreach (var update in pipeline.Updates)
        {
            table.AddRow(
                $"[dodgerblue1]{update.Quality.EscapeMarkup()}[/]",
                Change(update.Minimum),
                Change(update.Maximum),
                Change(update.Preferred)
            );
        }

        return table;

        static string Change(ValueChangeResponseOfQualitySizeValueResponse? change) =>
            change is null
                ? "[dim]unchanged[/]"
                : $"{QualitySizeText.Of(change.Current)} -> {QualitySizeText.Of(change.Desired)}";
    }

    private static Table? Fields(IReadOnlyList<FieldChange> changes) =>
        changes.Count == 0 ? null : FieldTable(changes);

    private static Table FieldTable(IEnumerable<FieldChange> changes)
    {
        var table = new Table()
            .AddColumn("[bold]Field[/]")
            .AddColumn("[bold]Current[/]")
            .AddColumn("[bold]New[/]");

        foreach (var change in changes)
        {
            table.AddRow(
                change.Field.EscapeMarkup(),
                change.Current.EscapeMarkup(),
                change.Desired.EscapeMarkup()
            );
        }

        return table;
    }

    private sealed record CfRow(
        string Action,
        TrashIdNameResponse Identity,
        CustomFormatSelectionResponse? Provenance
    );
}
