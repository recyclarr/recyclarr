using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Common;
using Recyclarr.ServarrApi.Tag;

namespace Recyclarr.Cli.Pipelines.Tags;

[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification =
    "Context objects are similar to DTOs; for usability we want to assign not append")]
public class TagPipelineContext : IPipelineContext
{
    public string PipelineDescription => "Tag Pipeline";
    public IReadOnlyCollection<SupportedServices> SupportedServiceTypes { get; } = new[]
    {
        SupportedServices.Sonarr
    };

    public IList<string> ConfigOutput { get; set; } = default!;
    public IList<SonarrTag> ApiFetchOutput { get; set; } = default!;
    public IList<string> TransactionOutput { get; set; } = default!;
}
