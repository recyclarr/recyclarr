using Recyclarr.Cli.Console.Widgets;
using Recyclarr.Client.V1;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Processors.Sync;

/// <summary>
/// Renders the config problems a sync job reports: files that could not be read or parsed, and
/// instances that were filtered out. Sibling of <see cref="ConfigFilterRendering"/>, which renders
/// the same information for commands that still load configs in-process.
/// </summary>
internal class ConfigDiagnosticsRenderer(IAnsiConsole console)
{
    public void Render(ConfigDiagnosticsResponse diagnostics)
    {
        RenderPanel(diagnostics);
        RenderFilterResults(diagnostics);
    }

    // Parse failures and deprecations read as a list of messages, matching how the sync run's own
    // diagnostics are presented.
    private void RenderPanel(ConfigDiagnosticsResponse diagnostics)
    {
        var panel = new DiagnosticPanel("Config Diagnostics");

        foreach (var file in diagnostics.MissingConfigFiles ?? [])
        {
            panel.AddError(null, $"Config file not found: {file}");
        }

        foreach (var failure in diagnostics.ParseFailures ?? [])
        {
            panel.AddError(failure.FileName ?? "unknown", failure.Message);
        }

        foreach (var message in diagnostics.DeprecationWarnings ?? [])
        {
            panel.AddDeprecation(null, message);
        }

        panel.Render(console);
    }

    // Instance-level filtering keeps the tree layout of the in-process renderers, since the shape
    // of the information (grouped by instance or base URL) is what makes it readable.
    private void RenderFilterResults(ConfigDiagnosticsResponse diagnostics)
    {
        var trees = new List<IRenderable>();

        if (diagnostics.UnknownInstances is { Count: > 0 } unknown)
        {
            var tree = new Tree("[orange1]Non-Existent Instances[/]");
            foreach (var instance in unknown)
            {
                tree.AddNode($"[white]{instance.EscapeMarkup()}[/]");
            }

            if (diagnostics.AvailableInstances is { Count: > 0 } available)
            {
                var availableNode = tree.AddNode("[dim]Available instances:[/]");
                foreach (var instance in available)
                {
                    availableNode.AddNode($"[cornflowerblue]{instance.EscapeMarkup()}[/]");
                }
            }
            else
            {
                tree.AddNode("[dim]No instances are configured.[/]");
            }

            trees.Add(tree);
        }

        if (diagnostics.InvalidInstances is { Count: > 0 } invalid)
        {
            var tree = new Tree("[orange1]Invalid Instances[/]");
            foreach (var instance in invalid)
            {
                var node = tree.AddNode(
                    $"[cornflowerblue]{instance.InstanceName.EscapeMarkup()}[/]"
                );
                foreach (var error in instance.Errors)
                {
                    node.AddNode($"[red]X[/] {error.EscapeMarkup()}");
                }
            }

            trees.Add(tree);
        }

        if (diagnostics.DuplicateInstances is { Count: > 0 } duplicates)
        {
            var tree = new Tree("[orange1]Duplicate Instances[/]");
            tree.AddNodes(duplicates.Select(x => x.EscapeMarkup()));
            trees.Add(tree);
        }

        if (diagnostics.SplitInstanceGroups is { Count: > 0 } split)
        {
            var tree = new Tree("[orange1]Split Instances[/]");
            foreach (var group in split)
            {
                var groupTree = new Tree(
                    $"[cornflowerblue]Base URL:[/] {group.BaseUrl.EscapeMarkup()}"
                );
                groupTree.AddNodes(group.InstanceNames.Select(x => x.EscapeMarkup()));
                tree.AddNode(groupTree);
            }

            trees.Add(tree);
        }

        if (trees.Count == 0)
        {
            return;
        }

        var padded = trees.Select(x => new Padder(x).Padding(0, 0, 0, 1));
        var panel = new Panel(new Padder(new Rows(padded).Collapse()).PadBottom(0))
            .Collapse()
            .Header("[red]Configuration Errors[/]")
            .RoundedBorder();

        console.Write(new Columns(panel));
    }
}
