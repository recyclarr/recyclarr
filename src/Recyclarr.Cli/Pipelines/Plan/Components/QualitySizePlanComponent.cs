using Recyclarr.Common.Extensions;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class QualitySizePlanComponent(
    QualitySizeResourceQuery guide,
    IServiceConfiguration config,
    DetectedService detectedService,
    ILogger log
) : IPlanComponent
{
    public void Process(PipelinePlan plan)
    {
        if (
            config.ServiceType == SupportedServices.Sonarr
            && new SonarrCapabilities(detectedService.Version).QualitySizesPerProfile
        )
        {
            PlanPerProfileSizes(plan);
            return;
        }

        WarnIgnoredProfileSizeSources(plan);
        PlanGlobalSizes(plan);
    }

    private void PlanGlobalSizes(PipelinePlan plan)
    {
        var configSizeData = config.QualityDefinition;
        if (configSizeData is null)
        {
            log.Debug("No quality_definition configured, skipping quality sizes");
            return;
        }

        var sizes = ResolveSizes(configSizeData, plan, errorPrefix: "");
        if (sizes is not null)
        {
            plan.QualitySizes = sizes;
        }
    }

    // Sizes live on quality profile items; only profiles listed under `quality_profiles` receive
    // them. Precedence: profile YAML, then the guide profile's `trash_quality_definition`
    // reference (a quality size trash_id), then the instance `quality_definition`.
    private void PlanPerProfileSizes(PipelinePlan plan)
    {
        var explicitProfiles = plan.QualityProfiles.Where(x => x.ShouldCreate).ToList();

        if (config.QualityDefinition is not null && explicitProfiles.Count == 0)
        {
            plan.AddWarning(
                "This Sonarr version stores quality sizes per quality profile. "
                    + "`quality_definition` only applies to profiles listed under "
                    + "`quality_profiles`, and none are configured."
            );
        }

        foreach (var profile in explicitProfiles)
        {
            var errorPrefix = $"Quality profile '{profile.Name}': ";
            var source = SelectSizeSource(profile, plan, errorPrefix);
            if (source is null)
            {
                continue;
            }

            var sizes = ResolveSizes(source, plan, errorPrefix);
            if (sizes is not null)
            {
                plan.AddQualityProfile(profile with { QualitySizes = sizes });
            }
        }
    }

    private QualityDefinitionConfig? SelectSizeSource(
        PlannedQualityProfile profile,
        PipelinePlan plan,
        string errorPrefix
    )
    {
        if (profile.Config.QualityDefinition is not null)
        {
            return profile.Config.QualityDefinition;
        }

        var guideReference = profile.GuideResource?.TrashQualityDefinition;
        if (guideReference is null || guideReference.Count == 0)
        {
            return config.QualityDefinition;
        }

        if (guideReference.Count > 1)
        {
            plan.AddError(
                $"{errorPrefix}The guide profile references more than one quality definition: "
                    + string.Join(", ", guideReference.Keys)
            );
            return null;
        }

        var (label, guideTrashId) = guideReference.First();
        var guideSizes = guide
            .Get(config.ServiceType)
            .LastOrDefault(x => x.TrashId.EqualsIgnoreCase(guideTrashId));

        if (guideSizes is null)
        {
            plan.AddError(
                $"{errorPrefix}The guide profile references a quality definition that does not "
                    + $"exist: {label} ({guideTrashId})"
            );
            return null;
        }

        return new QualityDefinitionConfig { Type = guideSizes.Type };
    }

    // Only user YAML warns; guide references are ignored silently because users cannot change
    // guide data.
    private static void WarnIgnoredProfileSizeSources(PipelinePlan plan)
    {
        var profiles = plan.QualityProfiles.Where(x =>
            x.ShouldCreate && x.Config.QualityDefinition is not null
        );

        foreach (var profile in profiles)
        {
            plan.AddWarning(
                $"Quality profile '{profile.Name}': this service version does not store quality "
                    + "sizes per profile, so the profile's `quality_definition` is ignored"
            );
        }
    }

    private PlannedQualitySizes? ResolveSizes(
        QualityDefinitionConfig configSizeData,
        PipelinePlan plan,
        string errorPrefix
    )
    {
        var preferredRatio = ClampPreferredRatio(configSizeData.PreferredRatio, plan);

        var guideSizeData = guide
            .Get(config.ServiceType)
            .LastOrDefault(x => x.Type.EqualsIgnoreCase(configSizeData.Type));

        if (guideSizeData is null)
        {
            plan.AddError(
                $"{errorPrefix}The specified quality definition type does not exist: "
                    + configSizeData.Type
            );
            return null;
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
        if (plannedQualities.Any(quality => !ValidateQualitySizeOrder(quality, plan)))
        {
            return null;
        }

        // Check for overrides that don't match any guide quality
        foreach (var configQuality in configSizeData.Qualities)
        {
            if (!guideSizeData.Qualities.Any(x => x.Quality.EqualsIgnoreCase(configQuality.Name)))
            {
                plan.AddError(
                    $"{errorPrefix}Quality '{configQuality.Name}' does not exist in the guide "
                        + $"for type '{configSizeData.Type}'"
                );
            }
        }

        return new PlannedQualitySizes
        {
            Type = configSizeData.Type,
            PreferredRatio = preferredRatio,
            Qualities = plannedQualities,
        };
    }

    private static decimal? ClampPreferredRatio(decimal? ratio, PipelinePlan diagnostics)
    {
        if (ratio is not (< 0 or > 1))
        {
            return ratio;
        }

        var clamped = Math.Clamp(ratio.Value, min: 0, max: 1);
        diagnostics.AddWarning(
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
        PipelinePlan diagnostics
    )
    {
        // preferred null = unlimited, so min is always ≤ preferred when preferred is unlimited
        if (quality.Preferred is not null && quality.Min > quality.Preferred)
        {
            diagnostics.AddError(
                $"Quality '{quality.Quality}': min ({quality.Min}) cannot be greater than preferred ({quality.Preferred})"
            );
            return false;
        }

        // max null = unlimited, so preferred is always ≤ max when max is unlimited
        // But if preferred is unlimited (null) and max is not, that's invalid
        if (quality.Preferred is null && quality.Max is not null)
        {
            diagnostics.AddError(
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
            diagnostics.AddError(
                $"Quality '{quality.Quality}': preferred ({quality.Preferred}) cannot be greater than max ({quality.Max})"
            );
            return false;
        }

        return true;
    }
}
