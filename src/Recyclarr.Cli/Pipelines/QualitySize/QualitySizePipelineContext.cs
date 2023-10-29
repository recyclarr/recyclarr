using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Common;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize;

[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification =
    "Context objects are similar to DTOs; for usability we want to assign not append")]
public class QualitySizePipelineContext : IPipelineContext
{
    public string PipelineDescription => "Quality Definition Pipeline";
    public IReadOnlyCollection<SupportedServices> SupportedServiceTypes { get; } = new[]
    {
        SupportedServices.Sonarr,
        SupportedServices.Radarr
    };

    public QualitySizeData? ConfigOutput { get; set; }
    public IList<ServiceQualityDefinitionItem> ApiFetchOutput { get; set; } = default!;
    public IList<ServiceQualityDefinitionItem> TransactionOutput { get; set; } = default!;
    public string? ConfigError { get; set; }
}
