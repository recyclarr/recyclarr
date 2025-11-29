using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizeConfigPhase(
    ILogger log,
    QualitySizeResourceQuery guide,
    IServiceConfiguration config,
    IQualityItemLimitFactory limitFactory
) : IPipelinePhase<QualitySizePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualitySizePipelineContext context,
        CancellationToken ct
    )
    {
        var configSizeData = config.QualityDefinition;
        if (configSizeData is null)
        {
            log.Debug("{Instance} has no quality definition", config.InstanceName);
            return PipelineFlow.Terminate;
        }

        ClampPreferredRatio(configSizeData);

        IEnumerable<QualitySizeResource> qualitySizes = config.ServiceType switch
        {
            SupportedServices.Radarr => guide.GetRadarr(),
            SupportedServices.Sonarr => guide.GetSonarr(),
            _ => throw new InvalidOperationException($"Unknown service type: {config.ServiceType}"),
        };

        var guideSizeData = qualitySizes.LastOrDefault(x =>
            x.Type.EqualsIgnoreCase(configSizeData.Type)
        );

        if (guideSizeData == null)
        {
            log.Error(
                "The specified quality definition type does not exist: {Type}",
                configSizeData.Type
            );
            return PipelineFlow.Terminate;
        }

        var itemLimits = await limitFactory.Create(config.ServiceType, ct);

        if (!ValidateAndApplyQualityOverrides(configSizeData, guideSizeData.Qualities, itemLimits))
        {
            return PipelineFlow.Terminate;
        }

        var sizeDataWithThresholds = guideSizeData
            .Qualities.Select(x => new QualityItemWithLimits(x, itemLimits))
            .ToList();

        if (sizeDataWithThresholds.Count == 0)
        {
            log.Debug("No Quality Definitions to process");
            return PipelineFlow.Terminate;
        }

        AdjustPreferredRatio(configSizeData, sizeDataWithThresholds);

        context.QualitySizeType = configSizeData.Type;
        context.Qualities = sizeDataWithThresholds;
        return PipelineFlow.Continue;
    }

    private void ClampPreferredRatio(QualityDefinitionConfig configSizeData)
    {
        if (configSizeData.PreferredRatio is not (< 0 or > 1))
        {
            return;
        }

        // Fix an out of range ratio and warn the user
        var clampedRatio = Math.Clamp(configSizeData.PreferredRatio.Value, 0, 1);

        log.Warning(
            "Your `preferred_ratio` of {CurrentRatio} is out of range. "
                + "It must be a decimal between 0.0 and 1.0. It has been clamped to {ClampedRatio}",
            configSizeData.PreferredRatio,
            clampedRatio
        );

        configSizeData.PreferredRatio = clampedRatio;
    }

    private void AdjustPreferredRatio(
        QualityDefinitionConfig configSizeData,
        List<QualityItemWithLimits> guideSizeData
    )
    {
        if (configSizeData.PreferredRatio is null)
        {
            return;
        }

        log.Information(
            "Using an explicit preferred ratio which will override values from the guide"
        );

        // Apply a calculated preferred size
        foreach (var quality in guideSizeData)
        {
            quality.Item.Preferred = quality.InterpolatedPreferred(
                configSizeData.PreferredRatio.Value
            );
        }
    }

    private bool ValidateAndApplyQualityOverrides(
        QualityDefinitionConfig configSizeData,
        IReadOnlyCollection<QualityItem> guideQualities,
        QualityItemLimits limits
    )
    {
        if (configSizeData.Qualities.Count == 0)
        {
            return true;
        }

        var guideQualityLookup = guideQualities.ToDictionary(
            x => x.Quality,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var configQuality in configSizeData.Qualities)
        {
            if (!guideQualityLookup.TryGetValue(configQuality.Name, out var guideQuality))
            {
                log.Error(
                    "Quality '{QualityName}' does not exist in the guide for type '{Type}'",
                    configQuality.Name,
                    configSizeData.Type
                );
                return false;
            }

            ApplyQualityOverride(guideQuality, configQuality, limits);

            log.Debug(
                "Applied quality override for {QualityName}: Min={Min}, Max={Max}, Preferred={Preferred}",
                configQuality.Name,
                guideQuality.Min,
                guideQuality.Max,
                guideQuality.Preferred
            );

            if (!ValidateQualitySizeOrder(guideQuality, configQuality.Name))
            {
                return false;
            }
        }

        return true;
    }

    private static void ApplyQualityOverride(
        QualityItem guideQuality,
        QualityDefinitionItemConfig configQuality,
        QualityItemLimits limits
    )
    {
        if (configQuality.Min is not null)
        {
            guideQuality.Min = ResolveQualitySizeValue(configQuality.Min, limits.MaxLimit);
        }

        if (configQuality.Max is not null)
        {
            guideQuality.Max = ResolveQualitySizeValue(configQuality.Max, limits.MaxLimit);
        }

        if (configQuality.Preferred is not null)
        {
            guideQuality.Preferred = ResolveQualitySizeValue(
                configQuality.Preferred,
                limits.PreferredLimit
            );
        }
    }

    private static decimal ResolveQualitySizeValue(QualitySizeValue value, decimal unlimitedValue)
    {
        return value switch
        {
            QualitySizeValue.Numeric numeric => numeric.Value,
            QualitySizeValue.Unlimited => unlimitedValue,
            _ => throw new InvalidOperationException($"Unknown QualitySizeValue type: {value}"),
        };
    }

    private bool ValidateQualitySizeOrder(QualityItem quality, string qualityName)
    {
        if (quality.Min > quality.Preferred)
        {
            log.Error(
                "Quality '{QualityName}': min ({Min}) cannot be greater than preferred ({Preferred})",
                qualityName,
                quality.Min,
                quality.Preferred
            );
            return false;
        }

        if (quality.Preferred > quality.Max)
        {
            log.Error(
                "Quality '{QualityName}': preferred ({Preferred}) cannot be greater than max ({Max})",
                qualityName,
                quality.Preferred,
                quality.Max
            );
            return false;
        }

        return true;
    }
}
