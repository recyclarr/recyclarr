using System.Diagnostics.CodeAnalysis;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize;

[SuppressMessage(
    "Usage",
    "CA2227:Collection properties should be read only",
    Justification = "Context objects are similar to DTOs; for usability we want to assign not append"
)]
internal class QualitySizePipelineContext : PipelineContext
{
    public override string PipelineDescription => "Quality Definition";

    public string QualitySizeType { get; set; } = "";
    public IReadOnlyCollection<QualityItemWithLimits> Qualities { get; set; } = [];
    public IList<ServiceQualityDefinitionItem> ApiFetchOutput { get; set; } = null!;
    public IList<ServiceQualityDefinitionItem> TransactionOutput { get; set; } = null!;
}
