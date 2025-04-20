using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config.Parsing;

namespace Recyclarr.Config.Filtering;

[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI-like properties should not be strings")]
public record SplitInstanceErrorInfo(string BaseUrl, IReadOnlyCollection<string> InstanceNames);

public class SplitInstancesFilter(ILogger log) : IConfigFilter
{
    public IReadOnlyCollection<LoadedConfigYaml> Filter(
        ConfigFilterCriteria criteria,
        IReadOnlyCollection<LoadedConfigYaml> configs,
        FilterContext context
    )
    {
        var splitInstances = configs
            .Where(x => x.Yaml.BaseUrl is not null)
            .GroupBy(x => x.Yaml.BaseUrl!)
            .Where(x => x.Count() > 1)
            .Select(x => new SplitInstanceErrorInfo(x.Key, x.Select(y => y.InstanceName).ToList()))
            .ToList();

        if (splitInstances.Count != 0)
        {
            context.AddResult(new SplitInstancesFilterResult(splitInstances));
            log.Debug(
                "Split instances: {@Instances}",
                // Anonymous object to avoid "$type" property in logs
                splitInstances.Select(x => new { x.BaseUrl, x.InstanceNames })
            );
        }

        return configs
            .ExceptBy(
                splitInstances.SelectMany(x => x.InstanceNames),
                x => x.InstanceName,
                StringComparer.InvariantCultureIgnoreCase
            )
            .ToList();
    }
}
