using Recyclarr.Config.Filtering;

namespace Recyclarr.Server.Sync;

// Projects parse failures, filter diagnostics, and deprecations into the shape used for startup
// logging and validation.
internal static class ConfigLoadDiagnosticsBuilder
{
    public static ConfigLoadDiagnostics Build(ServerConfigLoadResult result)
    {
        var parseFailures = result
            .Failures.Select(f => new ConfigParseFailure(f.FilePath?.Name, f.Line, f.Message))
            .ToList();

        var invalidInstances = new List<InvalidInstance>();
        var duplicateInstances = new List<string>();
        var splitGroups = new List<SplitInstanceGroup>();

        foreach (var filterResult in result.FilterResults)
        {
            switch (filterResult)
            {
                case InvalidInstancesFilterResult r:
                    invalidInstances.AddRange(
                        r.InvalidInstances.Select(i => new InvalidInstance(
                            i.InstanceName,
                            i.Failures.Select(f => f.ErrorMessage).ToList()
                        ))
                    );
                    break;

                case DuplicateInstancesFilterResult r:
                    duplicateInstances.AddRange(r.DuplicateInstances);
                    break;

                case SplitInstancesFilterResult r:
                    splitGroups.AddRange(
                        r.SplitInstances.Select(s => new SplitInstanceGroup(
                            s.BaseUrl,
                            s.InstanceNames.ToList()
                        ))
                    );
                    break;
            }
        }

        return new ConfigLoadDiagnostics
        {
            ParseFailures = parseFailures,
            InvalidInstances = invalidInstances,
            DuplicateInstances = duplicateInstances,
            SplitInstanceGroups = splitGroups,
            DeprecationWarnings = result.DeprecationWarnings.ToList(),
        };
    }
}
