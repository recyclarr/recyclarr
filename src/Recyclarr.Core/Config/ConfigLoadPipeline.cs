using Recyclarr.Config.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Config;

public interface IFilterResult
{
    void Render(IRenderable parent);
}

public interface IConfigFilter
{
    IReadOnlyCollection<LoadedConfigYaml> Filter(
        IReadOnlyCollection<LoadedConfigYaml> configs,
        FilterContext context
    );
}

public class FilterContext
{
    private readonly List<IFilterResult> _results = [];

    public IReadOnlyCollection<IFilterResult> Results => _results;

    public void AddResult(IFilterResult result) => _results.Add(result);
}

public class DuplicateInstancesFilterResult(IReadOnlyCollection<string> duplicateInstances)
    : IFilterResult
{
    public void Render(IRenderable parent)
    {
        throw new NotImplementedException();
    }
}

public class DuplicateInstancesFilter : IConfigFilter
{
    public IReadOnlyCollection<LoadedConfigYaml> Filter(
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
