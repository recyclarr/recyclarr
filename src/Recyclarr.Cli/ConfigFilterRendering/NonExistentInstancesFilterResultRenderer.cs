using Recyclarr.Config.Filtering;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.ConfigFilterRendering;

internal sealed class NonExistentInstancesFilterResultRenderer
    : TypedFilterResultRenderer<NonExistentInstancesFilterResult>
{
    protected override IRenderable RenderResults(NonExistentInstancesFilterResult filterResult)
    {
        var tree = new Tree("[orange1]Non-Existent Instances[/]");

        foreach (var instance in filterResult.NonExistentInstances)
        {
            tree.AddNode($"[white]{instance}[/]");
        }

        if (filterResult.AvailableInstances.Count > 0)
        {
            var availableNode = tree.AddNode("[dim]Available instances:[/]");
            foreach (var available in filterResult.AvailableInstances)
            {
                availableNode.AddNode($"[cornflowerblue]{available}[/]");
            }
        }
        else
        {
            tree.AddNode("[dim]No instances are configured.[/]");
        }

        return tree;
    }
}
