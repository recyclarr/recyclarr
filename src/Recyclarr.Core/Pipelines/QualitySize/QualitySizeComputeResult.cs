using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines.QualitySize;

internal record QualitySizeComputeResult(
    IReadOnlyCollection<UpdatedQualityItem> Items,
    QualityItemLimits? Limits,
    string QualityDefinitionType,
    QualitySizePipelineResult Result
) : IPipelineResultSource
{
    PipelineResult IPipelineResultSource.Result => Result;
}
