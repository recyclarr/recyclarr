using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync.Events;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class QualitySizePlanComponent(
    QualitySizeResourceQuery guide,
    IServiceConfiguration config,
    ILogger log
) : IPlanComponent
{
    public void Process(PipelinePlan plan, ISyncEventPublisher events)
    {
        var configSizeData = config.QualityDefinition;
        if (configSizeData is null)
        {
            log.Debug("No quality_definition configured, skipping quality sizes");
            return;
        }

        var preferredRatio = ClampPreferredRatio(configSizeData.PreferredRatio, events);

        var guideSizeData = guide
            .Get(config.ServiceType)
            .LastOrDefault(x => x.Type.EqualsIgnoreCase(configSizeData.Type));

        if (guideSizeData is null)
        {
            events.AddError(
                $"The specified quality definition type does not exist: {configSizeData.Type}"
            );
            return;
        }

        var overrideLookup = configSizeData.Qualities.ToDictionary(
            x => x.Name,
            StringComparer.OrdinalIgnoreCase
        );

        // Merge guide values with user overrides
        var plannedQualities = guideSizeData
            .Qualities.Select(q => MergeQuality(q, overrideLookup))
            .ToList();

        // Validate min ≤ preferred ≤ max ordering for each quality
        if (plannedQualities.Any(quality => !ValidateQualitySizeOrder(quality, events)))
        {
            return;
        }

        // Check for overrides that don't match any guide quality
        foreach (var configQuality in configSizeData.Qualities)
        {
            if (!guideSizeData.Qualities.Any(x => x.Quality.EqualsIgnoreCase(configQuality.Name)))
            {
                events.AddError(
                    $"Quality '{configQuality.Name}' does not exist in the guide for type '{configSizeData.Type}'"
                );
            }
        }

        plan.QualitySizes = new PlannedQualitySizes
        {
            Type = configSizeData.Type,
            PreferredRatio = preferredRatio,
            Qualities = plannedQualities,
        };
    }

    private static decimal? ClampPreferredRatio(decimal? ratio, ISyncEventPublisher events)
    {
        if (ratio is not (< 0 or > 1))
        {
            return ratio;
        }

        var clamped = Math.Clamp(ratio.Value, min: 0, max: 1);
        events.AddWarning(
            $"preferred_ratio of {ratio} is out of range (0.0-1.0), clamped to {clamped}"
        );
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
        ISyncEventPublisher events
    )
    {
        // preferred null = unlimited, so min is always ≤ preferred when preferred is unlimited
        if (quality.Preferred is not null && quality.Min > quality.Preferred)
        {
            events.AddError(
                $"Quality '{quality.Quality}': min ({quality.Min}) cannot be greater than preferred ({quality.Preferred})"
            );
            return false;
        }

        // max null = unlimited, so preferred is always ≤ max when max is unlimited
        // But if preferred is unlimited (null) and max is not, that's invalid
        if (quality.Preferred is null && quality.Max is not null)
        {
            events.AddError(
                $"Quality '{quality.Quality}': preferred (unlimited) cannot be greater than max ({quality.Max})"
            );
            return false;
        }

        if (
            quality.Preferred is not null
            && quality.Max is not null
            && quality.Preferred > quality.Max
        )
        {
            events.AddError(
                $"Quality '{quality.Quality}': preferred ({quality.Preferred}) cannot be greater than max ({quality.Max})"
            );
            return false;
        }

        return true;
    }
}
