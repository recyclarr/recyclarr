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
        ConfigFilterCriteria criteria,
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

public class DuplicateInstancesFilter : IConfigFilter
{
    private class Result(IReadOnlyCollection<string> duplicateInstances) : IFilterResult
    {
        public void Render(IRenderable parent)
        {
            throw new NotImplementedException();
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

public class NonExistentInstancesFilter : IConfigFilter
{
    private class Result(IReadOnlyCollection<string> nonExistentInstances) : IFilterResult
    {
        public void Render(IRenderable parent)
        {
            throw new NotImplementedException();
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
