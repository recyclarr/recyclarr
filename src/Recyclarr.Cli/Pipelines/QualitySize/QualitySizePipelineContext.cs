using System.Diagnostics.CodeAnalysis;
using Recyclarr.Compatibility;
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

    // Sonarr v5 stores sizes on quality profile items; the quality profile pipeline owns them.
    public static bool AppliesTo(ServiceApiTarget target) => target != ServiceApiTarget.SonarrV5;

    public override string PipelineDescription => "Quality Definition";
    public override bool ShouldSkip => !Plan.QualitySizesAvailable;

    public IReadOnlyList<QualityDefinitionItem> ApiFetchOutput { get; set; } = null!;
    public QualityItemLimits Limits { get; set; } = null!;
    public string QualityDefinitionType { get; set; } = null!;
    public IReadOnlyCollection<UpdatedQualityItem> TransactionOutput { get; set; } = [];
}
