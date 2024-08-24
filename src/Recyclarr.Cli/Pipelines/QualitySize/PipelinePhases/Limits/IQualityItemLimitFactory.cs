using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;

public interface IQualityItemLimitFactory
{
    Task<QualityItemLimits> Create(SupportedServices serviceType, CancellationToken ct);
}
