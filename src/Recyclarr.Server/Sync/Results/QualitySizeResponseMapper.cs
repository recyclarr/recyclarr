using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Sync.Results;

internal static class QualitySizeResponseMapper
{
    public static QualitySizePipelineResponse ToResponse(QualitySizePipelineResult result)
    {
        var updates = result.Deltas.Select(MapUpdate).ToList();

        return new QualitySizePipelineResponse
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = result.Outcomes.Select(x => OutcomeResponseMapper.Map(x)).ToList(),
            Updates = updates,
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };
    }

    public static QualitySizeUpdateResponse MapUpdate(QualitySizeDelta delta)
    {
        var response = new QualitySizeUpdateResponse
        {
            Quality = delta.Quality,
            Minimum = delta
                .Components.OfType<QualitySizeMinimumChanged>()
                .Select(x => ResultValueMapper.MapQualitySizeValue(x.Value))
                .SingleOrDefault(),
            Preferred = delta
                .Components.OfType<QualitySizePreferredChanged>()
                .Select(x => ResultValueMapper.MapQualitySizeValue(x.Value))
                .SingleOrDefault(),
            Maximum = delta
                .Components.OfType<QualitySizeMaximumChanged>()
                .Select(x => ResultValueMapper.MapQualitySizeValue(x.Value))
                .SingleOrDefault(),
        };
        ResultValueMapper.EnsureAllMapped(
            "Quality Size update component",
            delta.Components.Count,
            response.Minimum is null ? 0 : 1,
            response.Preferred is null ? 0 : 1,
            response.Maximum is null ? 0 : 1
        );
        return response;
    }
}
