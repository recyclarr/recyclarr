using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.Tags.Api;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Cli.Pipelines.Tags.PipelinePhases;

public class TagTransactionPhase
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification =
        "This non-static method establishes a pattern that will eventually become an interface")]
    public IList<string> Execute(IList<string> configTags, IList<SonarrTag> serviceTags)
    {
        // List of tags in config that do not already exist in the service. The goal is to figure out which tags need to
        // be created.
        return configTags
            .Where(ct => serviceTags.All(st => !st.Label.EqualsIgnoreCase(ct)))
            .ToList();
    }
}
