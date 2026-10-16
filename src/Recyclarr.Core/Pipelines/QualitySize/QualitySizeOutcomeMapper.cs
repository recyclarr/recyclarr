using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.QualitySize;

// Maps quality size plan outcomes to result outcomes. Shared by the global quality size operation
// and the quality profile operation, which reports sizes resolved per profile (Sonarr v5).
internal static class QualitySizeOutcomeMapper
{
    public static QualitySizeOutcome? Map(SyncOutcome outcome)
    {
        return outcome switch
        {
            QualityDefinitionNotFoundOutcome x => new QualitySizeDefinitionReferenceMismatchOutcome(
                x.Type
            ),
            QualityNotFoundOutcome x => new QualitySizeReferenceMismatchOutcome(x.Quality, x.Type),
            PreferredRatioClampedOutcome x => new QualitySizePreferredRatioClampedOutcome(
                new ValueDelta<decimal>(x.Original, x.Clamped)
            ),
            MinGreaterThanPreferredOutcome x => new QualitySizeMinimumGreaterThanPreferredOutcome(
                x.Quality,
                Numeric(x.Min),
                Numeric(x.Preferred)
            ),
            UnlimitedPreferredGreaterThanMaxOutcome x =>
                new QualitySizeUnlimitedPreferredGreaterThanMaximumOutcome(
                    x.Quality,
                    new QualitySizeValue.Unlimited(),
                    Numeric(x.Max)
                ),
            PreferredGreaterThanMaxOutcome x => new QualitySizePreferredGreaterThanMaximumOutcome(
                x.Quality,
                Numeric(x.Preferred),
                Numeric(x.Max)
            ),
            GuideQualityDefinitionNotFoundOutcome x => new QualitySizeGuideReferenceMismatchOutcome(
                x.Label,
                x.TrashId
            ),
            AmbiguousGuideQualityDefinitionOutcome x =>
                new QualitySizeAmbiguousGuideReferenceOutcome(x.Labels),
            _ => null,
        };
    }

    // A clamped ratio still produces usable sizes; every other outcome leaves sizes unapplied.
    public static bool IsIncomplete(QualitySizeOutcome outcome) =>
        outcome is not QualitySizePreferredRatioClampedOutcome;

    private static QualitySizeValue.Numeric Numeric(decimal value) => new(value);
}
