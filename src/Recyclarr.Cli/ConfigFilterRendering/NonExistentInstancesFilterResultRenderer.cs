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
        tree.AddNodes(filterResult.NonExistentInstances);
        return tree;
    }
}
