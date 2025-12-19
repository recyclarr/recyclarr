using System.Diagnostics.CodeAnalysis;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.Sync;
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
    public override PipelineType PipelineType => PipelineType.QualitySize;
    public override bool ShouldSkip => !Plan.QualitySizesAvailable;

    public IList<ServiceQualityDefinitionItem> ApiFetchOutput { get; set; } = null!;
    public QualityItemLimits Limits { get; set; } = null!;
    public string QualityDefinitionType { get; set; } = null!;
    public IReadOnlyCollection<UpdatedQualityItem> TransactionOutput { get; set; } = [];
}
