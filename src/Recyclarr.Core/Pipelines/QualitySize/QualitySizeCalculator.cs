using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines.QualitySize;

// Turns planned guide sizes into concrete values within the service's limits. Shared by the global
// quality definition pipeline and per-profile sizes so both produce identical values.
internal static class QualitySizeCalculator
{
    // Unlimited (null) values resolve to the service limit so values can be compared directly.
    public static (decimal Min, decimal Max, decimal Preferred) ResolveValues(
        PlannedQualityItem planned,
        decimal? preferredRatio,
        QualityItemLimits limits
    )
    {
        var min = planned.Min;
        var max = Math.Min(planned.Max ?? limits.MaxLimit, limits.MaxLimit);
        var preferred = Math.Min(planned.Preferred ?? limits.PreferredLimit, limits.PreferredLimit);

        if (preferredRatio is not null)
        {
            var cappedMax = Math.Min(max, limits.PreferredLimit);
            preferred = Math.Round(min + (cappedMax - min) * preferredRatio.Value, decimals: 1);
        }

        return (min, max, preferred);
    }

    // Services represent "unlimited" as null; values at or above the limit are sent as null.
    public static QualityItemSize ToServiceSize(
        (decimal Min, decimal Max, decimal Preferred) resolved,
        QualityItemLimits limits
    )
    {
        return new QualityItemSize(
            resolved.Min,
            resolved.Max < limits.MaxLimit ? resolved.Max : null,
            resolved.Preferred < limits.PreferredLimit ? resolved.Preferred : null
        );
    }
}
