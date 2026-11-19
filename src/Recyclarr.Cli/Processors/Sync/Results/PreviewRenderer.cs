using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync.Results;

/// <summary>
/// Renders what a preview sync would change as one tree per instance: a heading line with the
/// instance's change counts, then each pipeline, each changed resource, and each changed value.
/// Every pipeline is drawn from the same <see cref="PreviewChange"/> shape, so colors and symbols
/// mean the same thing everywhere: green "+" creates, yellow "~" updates, red "-" deletes, muted
/// text is context, and bold text is the new value. A totals line follows the last instance.
/// </summary>
internal class PreviewRenderer(IAnsiConsole console)
{
    private const string Accent = "dodgerblue1";
    private const string Create = "green";
    private const string Update = "yellow";
    private const string Delete = "red";
    private const string Muted = "grey";
    private static readonly Style Border = new(Color.Grey35);

    public void Render(IReadOnlyList<InstanceResults> instances)
    {
        var all = new List<PreviewChange>();

        foreach (var instance in instances)
        {
            var pipelines = PreviewChanges.Of(instance);
            var changes = pipelines.SelectMany(x => x.Changes).ToList();
            all.AddRange(changes);

            // The heading is the tree root so the guide lines hang directly off it.
            var tree = new Tree(Heading(instance.Name, changes))
                .Guide(TreeGuide.Line)
                .Style(Border);
            foreach (var pipeline in pipelines)
            {
                tree.AddNode(PipelineNode(pipeline));
            }

            console.WriteLine();
            console.Write(tree);
        }

        console.WriteLine();
        console.Write(new Rule().RuleStyle(Border));
        console.MarkupLine(Totals(all, instances.Count));
    }

    // Name at the left edge, then a rule filling the rest of the line.
    private static Grid Heading(string name, IReadOnlyList<PreviewChange> changes) =>
        new Grid { Expand = true }
            .AddColumn(new GridColumn().NoWrap().PadRight(1))
            .AddColumn(new GridColumn().PadLeft(0))
            .AddRow(
                new Markup(
                    $"[bold {Accent}]{name.EscapeMarkup()}[/] [{Muted}]preview[/]  {Counts(changes)}"
                ),
                new Rule().RuleStyle(Border)
            );

    private static TreeNode PipelineNode(PreviewPipeline pipeline)
    {
        if (pipeline.Skip is { } skip)
        {
            return new TreeNode(
                new Markup(
                    $"{pipeline.Title}  [{Delete}]{skip.Label}[/] [{Muted}]({skip.Reason})[/]"
                )
            );
        }

        if (pipeline.Changes.Count == 0)
        {
            return new TreeNode(new Markup($"[{Muted}]{pipeline.Title}  no changes[/]"));
        }

        var node = new TreeNode(new Markup($"[bold]{pipeline.Title}[/]"));
        var nameWidth = pipeline.Changes.Max(x => x.Name.Length);

        foreach (var change in pipeline.Changes)
        {
            var item = node.AddNode(ItemLine(change, nameWidth));
            var showCurrent = change.Kind != ChangeKind.Create;
            AddFields(item, change.Fields, showCurrent);

            foreach (var section in change.Sections)
            {
                var note = section.Note is null ? "" : $"  [{Muted}]{section.Note}[/]";
                var sectionNode = item.AddNode($"[{Accent}]{section.Title}[/]{note}");
                AddFields(sectionNode, section.Fields, showCurrent);
                AddLayout(sectionNode, section.Layout);
            }
        }

        return node;
    }

    private static string ItemLine(PreviewChange change, int nameWidth)
    {
        var name = change.Name.PadRight(nameWidth).EscapeMarkup();
        var detail = change.Detail is null ? "" : $"  [{Muted}]{change.Detail.EscapeMarkup()}[/]";
        return $"{Symbol(change.Kind)} {name}{detail}";
    }

    // Values align in columns: labels padded right, current values padded left so the arrows
    // line up. A created resource has no current values, so only the new value shows.
    private static void AddFields(
        TreeNode parent,
        IReadOnlyList<FieldChange> fields,
        bool showCurrent
    )
    {
        if (fields.Count == 0)
        {
            return;
        }

        var labelWidth = fields.Max(x => x.Field.Length);
        var currentWidth = fields.Max(x => x.Current.Length);
        var desiredWidth = fields.Max(x => x.Desired.Length);

        foreach (var field in fields)
        {
            var label = $"[{Muted}]{field.Field.PadRight(labelWidth).EscapeMarkup()}[/]";
            var desired = $"[bold]{field.Desired.PadRight(desiredWidth).EscapeMarkup()}[/]";
            var value = showCurrent
                ? $"[{Muted}]{field.Current.PadLeft(currentWidth).EscapeMarkup()} →[/] {desired}"
                : desired;
            var note = field.Note is null ? "" : $"  [{Muted}]{field.Note.EscapeMarkup()}[/]";

            parent.AddNode($"{label}  {value}{note}");
        }
    }

    private static void AddLayout(TreeNode section, IReadOnlyList<QualityLine> layout)
    {
        if (layout.Count == 0)
        {
            return;
        }

        var nameWidth = layout.Max(x => x.Name.Length);

        foreach (var line in layout)
        {
            var mark = line.Allowed ? $"[{Create}]✓[/]" : $"[{Delete}]✗[/]";
            var arrow = line.MovedUp switch
            {
                true => $" [{Update}]↑[/]",
                false => $" [{Update}]↓[/]",
                null => "  ",
            };
            var padding = new string(' ', nameWidth - line.Name.Length);
            var changed = line.Note is not null || line.MovedUp is not null;
            var name = changed
                ? line.Name.EscapeMarkup()
                : $"[{Muted}]{line.Name.EscapeMarkup()}[/]";
            var note = line.Note is null ? "" : $"  [{Update}]{line.Note.EscapeMarkup()}[/]";

            var node = section.AddNode($"{mark} {name}{arrow}{padding}{note}");
            foreach (var member in line.Members)
            {
                node.AddNode($"[{Muted}]{member.EscapeMarkup()}[/]");
            }
        }
    }

    private static string Symbol(ChangeKind kind) =>
        kind switch
        {
            ChangeKind.Create => $"[{Create}]+[/]",
            ChangeKind.Update => $"[{Update}]~[/]",
            ChangeKind.Delete => $"[{Delete}]-[/]",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    private static string Counts(IReadOnlyList<PreviewChange> changes)
    {
        if (changes.Count == 0)
        {
            return $"[{Muted}]no changes[/]";
        }

        return string.Join(
            " ",
            Enum.GetValues<ChangeKind>()
                .Select(kind => (kind, count: changes.Count(x => x.Kind == kind)))
                .Where(x => x.count > 0)
                .Select(x => $"{Symbol(x.kind)}[{Muted}]{x.count}[/]")
        );
    }

    private static string Totals(IReadOnlyList<PreviewChange> changes, int instanceCount)
    {
        var create = changes.Count(x => x.Kind == ChangeKind.Create);
        var update = changes.Count(x => x.Kind == ChangeKind.Update);
        var delete = changes.Count(x => x.Kind == ChangeKind.Delete);
        var instances = instanceCount == 1 ? "1 instance" : $"{instanceCount} instances";

        return $"[{Create}]{create} to create[/], [{Update}]{update} to update[/], "
            + $"[{Delete}]{delete} to delete[/] across {instances}. "
            + $"[{Muted}]Nothing was changed; run without --preview to apply.[/]";
    }
}
