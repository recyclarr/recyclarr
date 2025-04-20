using Recyclarr.Config.Filtering;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.ConfigFilterRendering;

internal sealed class DuplicateInstancesFilterResultRenderer
    : TypedFilterResultRenderer<DuplicateInstancesFilterResult>
{
    protected override IRenderable RenderResults(DuplicateInstancesFilterResult filterResult)
    {
        var tree = new Tree("[orange1]Duplicate Instances[/]");
        tree.AddNodes(filterResult.DuplicateInstances);
        return tree;
    }
}
