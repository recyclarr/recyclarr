using Recyclarr.Config.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Config.Filtering;

public class NonExistentInstancesFilter : IConfigFilter
{
    private class Result(IReadOnlyCollection<string> nonExistentInstances) : IFilterResult
    {
        public IRenderable Render()
        {
            var tree = new Tree("[orange1]Non-Existent Instances[/]");
            tree.AddNodes(nonExistentInstances);
            return tree;
        }
    }

    public IReadOnlyCollection<LoadedConfigYaml> Filter(
        ConfigFilterCriteria criteria,
        IReadOnlyCollection<LoadedConfigYaml> configs,
        FilterContext context
    )
    {
        if (criteria.Instances is { Count: > 0 })
        {
            var names = configs.Select(x => x.InstanceName).ToList();

            var nonExistentInstances = criteria
                .Instances.Where(x => !names.Contains(x, StringComparer.InvariantCultureIgnoreCase))
                .ToList();

            context.AddResult(new Result(nonExistentInstances));
        }

        return configs;
    }
}
