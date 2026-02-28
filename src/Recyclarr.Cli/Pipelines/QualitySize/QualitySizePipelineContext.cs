using System.Diagnostics.CodeAnalysis;
using Recyclarr.Servarr.QualitySize;
using Recyclarr.Sync;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize;

[SuppressMessage(
    "Usage",
    "CA2227:Collection properties should be read only",
    Justification = "Context objects are similar to DTOs; for usability we want to assign not append"
)]
internal class QualitySizePipelineContext : PipelineContext, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.QualitySize;
    public static IReadOnlyList<PipelineType> Dependencies => [];

    public override string PipelineDescription => "Quality Definition";
    public override bool ShouldSkip => Plan.QualitySizes is null;

    public IReadOnlyList<QualityDefinitionItem> ApiFetchOutput { get; set; } = null!;
    public QualityItemLimits Limits { get; set; } = null!;
    public string QualityDefinitionType { get; set; } = null!;
    public IReadOnlyCollection<UpdatedQualityItem> TransactionOutput { get; set; } = [];
}
