using Recyclarr.Config.Parsing;

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
