using Recyclarr.Config.Parsing;

namespace Recyclarr.Config.Filtering;

public interface IConfigFilter
{
    IReadOnlyCollection<LoadedConfigYaml> Filter(
        ConfigFilterCriteria criteria,
        IReadOnlyCollection<LoadedConfigYaml> configs,
        FilterContext context
    );
}
