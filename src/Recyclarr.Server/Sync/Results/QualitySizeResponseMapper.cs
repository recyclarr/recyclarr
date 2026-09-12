using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Sync.Results;

internal static class QualitySizeResponseMapper
{
    public static QualitySizePipelineResponse ToResponse(QualitySizePipelineResult result)
    {
        var outcomes = MapOutcomes(result.Outcomes);
        var updates = result.Deltas.Select(MapUpdate).ToList();
        ResultValueMapper.EnsureAllMapped(
            "Quality Size outcome",
            result.Outcomes.Count,
            outcomes.DefinitionReferenceMismatches.Count,
            outcomes.ReferenceMismatches.Count,
            outcomes.ServiceQualitiesNotFound.Count,
            outcomes.PreferredRatiosClamped.Count,
            outcomes.MinimumGreaterThanPreferred.Count,
            outcomes.UnlimitedPreferredGreaterThanMaximum.Count,
            outcomes.PreferredGreaterThanMaximum.Count
        );

        return new QualitySizePipelineResponse(
            ResultValueMapper.MapStatus(result.Status),
            outcomes,
            updates
        )
        {
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };
    }

    private static QualitySizeUpdateResponse MapUpdate(QualitySizeDelta delta)
    {
        var response = new QualitySizeUpdateResponse(delta.Quality)
        {
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

    private static QualitySizeOutcomesResponse MapOutcomes(
        IReadOnlyList<QualitySizeOutcome> outcomes
    ) =>
        new()
        {
            DefinitionReferenceMismatches = outcomes
                .OfType<QualitySizeDefinitionReferenceMismatchOutcome>()
                .Select(x => new QualitySizeTypeResponse(x.Type))
                .ToList(),
            ReferenceMismatches = outcomes
                .OfType<QualitySizeReferenceMismatchOutcome>()
                .Select(x => new QualitySizeReferenceResponse(x.Quality, x.Type))
                .ToList(),
            ServiceQualitiesNotFound = outcomes
                .OfType<QualitySizeServiceQualityNotFoundOutcome>()
                .Select(x => x.Quality)
                .ToList(),
            PreferredRatiosClamped = outcomes
                .OfType<QualitySizePreferredRatioClampedOutcome>()
                .Select(x => ResultValueMapper.MapValue(x.Value))
                .ToList(),
            MinimumGreaterThanPreferred = outcomes
                .OfType<QualitySizeMinimumGreaterThanPreferredOutcome>()
                .Select(x => new QualitySizeMinimumComparisonResponse(
                    x.Quality,
                    ResultValueMapper.MapQualitySize(x.Minimum),
                    ResultValueMapper.MapQualitySize(x.Preferred)
                ))
                .ToList(),
            UnlimitedPreferredGreaterThanMaximum = outcomes
                .OfType<QualitySizeUnlimitedPreferredGreaterThanMaximumOutcome>()
                .Select(x => new QualitySizeMaximumComparisonResponse(
                    x.Quality,
                    ResultValueMapper.MapQualitySize(x.Preferred),
                    ResultValueMapper.MapQualitySize(x.Maximum)
                ))
                .ToList(),
            PreferredGreaterThanMaximum = outcomes
                .OfType<QualitySizePreferredGreaterThanMaximumOutcome>()
                .Select(x => new QualitySizeMaximumComparisonResponse(
                    x.Quality,
                    ResultValueMapper.MapQualitySize(x.Preferred),
                    ResultValueMapper.MapQualitySize(x.Maximum)
                ))
                .ToList(),
        };
}
