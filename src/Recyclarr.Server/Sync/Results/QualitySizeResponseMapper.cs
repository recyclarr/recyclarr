using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Sync.Results;

internal static class QualitySizeResponseMapper
{
    public static QualitySizePipelineResponse ToResponse(QualitySizePipelineResult result) =>
        new()
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = result.Outcomes.Select(x => OutcomeResponseMapper.Map(x)).ToList(),
            Items = result.Resources.Select(ResultValueMapper.MapQualitySizeItem).ToList(),
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };
}
