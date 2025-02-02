using Recyclarr.Config.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Config.Filtering;

public class SplitInstancesFilter : IConfigFilter
{
    private class Result(IReadOnlyCollection<string> splitInstances) : IFilterResult
    {
        public IRenderable Render()
        {
            return Text.Empty;
        }
    }

    public IReadOnlyCollection<LoadedConfigYaml> Filter(
        ConfigFilterCriteria criteria,
        IReadOnlyCollection<LoadedConfigYaml> configs,
        FilterContext context
    )
    {
        var splitInstances = configs
            .GroupBy(x => x.Yaml.BaseUrl)
            .Where(x => x.Count() > 1)
            .SelectMany(x => x.Select(y => y.InstanceName))
            .ToList();

        if (splitInstances.Count != 0)
        {
            context.AddResult(new Result(splitInstances));
        }

        return configs
            .ExceptBy(
                splitInstances,
                x => x.InstanceName,
                StringComparer.InvariantCultureIgnoreCase
            )
            .ToList();
    }
}
