using Recyclarr.Config.Parsing;

namespace Recyclarr.Config.Filtering;

public record ConfigFilterProcessorResult(
    IReadOnlyCollection<LoadedConfigYaml> Configs,
    IReadOnlyCollection<IFilterResult> FilterResults
);

public class ConfigFilterProcessor(IEnumerable<IConfigFilter> filters)
{
    public ConfigFilterProcessorResult Filter(
        ConfigFilterCriteria criteria,
        IReadOnlyCollection<LoadedConfigYaml> configs,
        IReadOnlyCollection<string> allAvailableInstances
    )
    {
        var context = new FilterContext { AllAvailableInstances = allAvailableInstances };

        var filteredConfigs = filters.Aggregate(
            configs,
            (current, filter) => filter.Filter(criteria, current, context)
        );

        return new ConfigFilterProcessorResult(filteredConfigs, context.Results);
    }
}
