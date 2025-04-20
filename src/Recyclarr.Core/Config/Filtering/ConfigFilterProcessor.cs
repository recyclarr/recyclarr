using Recyclarr.Config.Parsing;

namespace Recyclarr.Config.Filtering;

public class ConfigFilterProcessor(
    IFilterResultRenderer renderer,
    IEnumerable<IConfigFilter> filters
)
{
    public IReadOnlyCollection<LoadedConfigYaml> FilterAndRender(
        ConfigFilterCriteria criteria,
        IReadOnlyCollection<LoadedConfigYaml> configs
    )
    {
        var context = new FilterContext();

        var filteredConfigs = filters.Aggregate(
            configs,
            (current, filter) => filter.Filter(criteria, current, context)
        );

        if (context.Results.Count > 0)
        {
            renderer.RenderResults(context.Results);
        }

        return filteredConfigs;
    }
}
