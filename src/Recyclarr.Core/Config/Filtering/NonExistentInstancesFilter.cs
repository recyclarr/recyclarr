using Recyclarr.Config.Parsing;

namespace Recyclarr.Config.Filtering;

public class NonExistentInstancesFilter(ILogger log) : IConfigFilter
{
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

            context.AddResult(new NonExistentInstancesFilterResult(nonExistentInstances));
            log.Debug("Non-existent instances: {Instances}", nonExistentInstances);
        }

        return configs;
    }
}
