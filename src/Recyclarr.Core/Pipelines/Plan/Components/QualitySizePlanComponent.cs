using Recyclarr.Common.Extensions;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines.Plan.Components;

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

        ReportIgnoredProfileSizeSources(plan);
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

        plan.QualitySizes = ResolveSizes(configSizeData, plan.Add);
    }

    // Sizes live on quality profile items; only profiles listed under `quality_profiles` receive
    // them. Precedence: profile YAML, then the guide profile's `trash_quality_definition`
    // reference (a quality size trash_id), then the instance `quality_definition`.
    private void PlanPerProfileSizes(PipelinePlan plan)
    {
        var explicitProfiles = plan.QualityProfiles.Where(x => x.ShouldCreate).ToList();

        if (config.QualityDefinition is not null && explicitProfiles.Count == 0)
        {
            plan.Add(new QualityDefinitionUnusedOutcome());
        }

        foreach (var profile in explicitProfiles)
        {
            void Report(SyncOutcome outcome) =>
                plan.Add(new QualityProfileSizeOutcome(profile, outcome));

            var source = SelectSizeSource(profile, Report);
            if (source is null)
            {
                continue;
            }

            plan.AddQualityProfile(profile with { QualitySizes = ResolveSizes(source, Report) });
        }
    }

    private QualityDefinitionConfig? SelectSizeSource(
        PlannedQualityProfile profile,
        Action<SyncOutcome> report
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
            report(new AmbiguousGuideQualityDefinitionOutcome(guideReference.Keys.ToList()));
            return null;
        }

        var (label, guideTrashId) = guideReference.First();
        var guideSizes = guide
            .Get(config.ServiceType)
            .LastOrDefault(x => x.TrashId.EqualsIgnoreCase(guideTrashId));

        if (guideSizes is null)
        {
            report(new GuideQualityDefinitionNotFoundOutcome(label, guideTrashId));
            return null;
        }

        return new QualityDefinitionConfig { Type = guideSizes.Type };
    }

    // Only user YAML is reported; guide references are ignored silently because users cannot
    // change guide data.
    private static void ReportIgnoredProfileSizeSources(PipelinePlan plan)
    {
        var profiles = plan.QualityProfiles.Where(x =>
            x.ShouldCreate && x.Config.QualityDefinition is not null
        );

        foreach (var profile in profiles)
        {
            plan.Add(new ProfileQualityDefinitionIgnoredOutcome(profile));
        }
    }

    private PlannedQualitySizes ResolveSizes(
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
