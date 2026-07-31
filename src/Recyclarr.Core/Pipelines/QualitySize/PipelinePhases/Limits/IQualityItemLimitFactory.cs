using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines.QualitySize.PipelinePhases.Limits;

internal interface IQualityItemLimitFactory
{
    Task<QualityItemLimits> Create(SupportedServices serviceType, CancellationToken ct);
}
