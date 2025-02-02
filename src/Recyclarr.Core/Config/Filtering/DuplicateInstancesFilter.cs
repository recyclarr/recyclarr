using Recyclarr.Config.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Config.Filtering;

public class DuplicateInstancesFilter : IConfigFilter
{
    private class Result(IReadOnlyCollection<string> duplicateInstances) : IFilterResult
    {
        public IRenderable Render()
        {
            return new Rows(
                [
                    new Markup("[orange1]Duplicate Instances[/]"),
                    .. duplicateInstances.Select(x => new Text($"• {x}")),
                ]
            );
        }
    }

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
            context.AddResult(new Result(duplicateInstances));
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
