using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines.Plan.Components;

/// <summary>
/// Resolves one configured size source against the guide: merges user overrides into the guide
/// sizes, clamps the preferred ratio, and drops qualities whose limits are out of order. Shared by
/// global quality size planning and per-profile size planning (Sonarr v5); it knows neither
/// destination. Problems are reported through the callback, and an unknown type yields an empty
/// size set.
/// </summary>
internal class QualitySizeResolver(QualitySizeResourceQuery guide, IServiceConfiguration config)
{
    public PlannedQualitySizes Resolve(
        QualityDefinitionConfig configSizeData,
        Action<SyncOutcome> report
    )
    {
        var preferredRatio = ClampPreferredRatio(configSizeData.PreferredRatio, report);

        var guideSizeData = guide
            .Get(config.ServiceType)
            .LastOrDefault(x => x.Type.EqualsIgnoreCase(configSizeData.Type));

        if (guideSizeData is null)
        {
            report(new QualityDefinitionNotFoundOutcome(configSizeData.Type));
            return new PlannedQualitySizes
            {
                Type = configSizeData.Type,
                PreferredRatio = preferredRatio,
                Qualities = [],
            };
        }

        var overrideLookup = configSizeData.Qualities.ToDictionary(
            x => x.Name,
            StringComparer.OrdinalIgnoreCase
        );

        // Merge guide values with user overrides
        var plannedQualities = guideSizeData
            .Qualities.Select(q => MergeQuality(q, overrideLookup))
            .ToList();

        var validQualities = new List<PlannedQualityItem>();
        foreach (var quality in plannedQualities)
        {
            if (ValidateQualitySizeOrder(quality, report))
            {
                validQualities.Add(quality);
            }
        }

        // Check for overrides that don't match any guide quality
        foreach (var configQuality in configSizeData.Qualities)
        {
            if (!guideSizeData.Qualities.Any(x => x.Quality.EqualsIgnoreCase(configQuality.Name)))
            {
                report(new QualityNotFoundOutcome(configQuality.Name, configSizeData.Type));
            }
        }

        return new PlannedQualitySizes
        {
            Type = configSizeData.Type,
            PreferredRatio = preferredRatio,
            Qualities = validQualities,
        };
    }

    private static decimal? ClampPreferredRatio(decimal? ratio, Action<SyncOutcome> report)
    {
        if (ratio is not (< 0 or > 1))
        {
            return ratio;
        }

        var clamped = Math.Clamp(ratio.Value, min: 0, max: 1);
        report(new PreferredRatioClampedOutcome(ratio.Value, clamped));
        return clamped;
    }

    private static PlannedQualityItem MergeQuality(
        QualityItem guideQuality,
        Dictionary<string, QualityDefinitionItemConfig> overrides
    )
    {
        var min = guideQuality.Min;
        var max = (decimal?)guideQuality.Max;
        var preferred = (decimal?)guideQuality.Preferred;

        if (overrides.TryGetValue(guideQuality.Quality, out var configOverride))
        {
            min = ResolveValue(configOverride.Min, guideQuality.Min);
            max = ResolveNullableValue(configOverride.Max, guideQuality.Max);
            preferred = ResolveNullableValue(configOverride.Preferred, guideQuality.Preferred);
        }

        return new PlannedQualityItem(guideQuality.Quality, min, max, preferred);
    }

    private static decimal ResolveValue(QualitySizeValue? configValue, decimal guideValue)
    {
        return configValue switch
        {
            QualitySizeValue.Numeric n => n.Value,
            QualitySizeValue.Unlimited => guideValue, // Min can't be unlimited, use guide
            null => guideValue,
            _ => guideValue,
        };
    }

    private static decimal? ResolveNullableValue(QualitySizeValue? configValue, decimal guideValue)
    {
        return configValue switch
        {
            QualitySizeValue.Numeric n => n.Value,
            QualitySizeValue.Unlimited => null, // null = unlimited
            null => guideValue, // No override, use guide value
            _ => guideValue,
        };
    }

    private static bool ValidateQualitySizeOrder(
        PlannedQualityItem quality,
        Action<SyncOutcome> report
    )
    {
        // preferred null = unlimited, so min is always ≤ preferred when preferred is unlimited
        if (quality.Preferred is not null && quality.Min > quality.Preferred)
        {
            report(
                new MinGreaterThanPreferredOutcome(
                    quality.Quality,
                    quality.Min,
                    quality.Preferred.Value
                )
            );
            return false;
        }

        // max null = unlimited, so preferred is always ≤ max when max is unlimited
        // But if preferred is unlimited (null) and max is not, that's invalid
        if (quality.Preferred is null && quality.Max is not null)
        {
            report(new UnlimitedPreferredGreaterThanMaxOutcome(quality.Quality, quality.Max.Value));
            return false;
        }

        if (
            quality.Preferred is not null
            && quality.Max is not null
            && quality.Preferred > quality.Max
        )
        {
            report(
                new PreferredGreaterThanMaxOutcome(
                    quality.Quality,
                    quality.Preferred.Value,
                    quality.Max.Value
                )
            );
            return false;
        }

        return true;
    }
}
