using Recyclarr.Config.Filtering;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.ConfigFilterRendering;

internal sealed class SplitInstancesFilterResultRenderer
    : TypedFilterResultRenderer<SplitInstancesFilterResult>
{
    protected override IRenderable RenderResults(SplitInstancesFilterResult filterResult)
    {
        var tree = new Tree("[orange1]Split Instances[/]");

        foreach (var (baseUrl, instanceNames) in filterResult.SplitInstances)
        {
            var instanceTree = new Tree($"[cornflowerblue]Base URL:[/] {baseUrl}");
            instanceTree.AddNodes(instanceNames);
            tree.AddNode(instanceTree);
        }

        return tree;
    }
}
