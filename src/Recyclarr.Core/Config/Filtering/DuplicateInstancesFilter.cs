using Recyclarr.Config.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Config.Filtering;

public class DuplicateInstancesFilter(ILogger log) : IConfigFilter
{
    public IReadOnlyCollection<LoadedConfigYaml> Filter(
        ConfigFilterCriteria criteria,
        IReadOnlyCollection<LoadedConfigYaml> configs,
        FilterContext context
    )
    {
        var duplicateInstances = configs
            .Select(x => x.InstanceName)
            .GroupBy(x => x, StringComparer.InvariantCultureIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.First())
            .ToList();

        if (duplicateInstances.Count != 0)
        {
            context.AddResult(new DuplicateInstancesFilterResult(duplicateInstances));
            log.Debug("Duplicate instances: {Instances}", duplicateInstances);
        }

        return configs
            .ExceptBy(
                duplicateInstances,
                x => x.InstanceName,
                StringComparer.InvariantCultureIgnoreCase
            )
            .ToList();
    }
}

public class DuplicateInstancesFilterResult(IReadOnlyCollection<string> duplicateInstances)
    : IFilterResult
{
    public IReadOnlyCollection<string> DuplicateInstances => duplicateInstances;

    public IRenderable Render()
    {
        var tree = new Tree("[orange1]Duplicate Instances[/]");
        tree.AddNodes(duplicateInstances);
        return tree;
    }
}
