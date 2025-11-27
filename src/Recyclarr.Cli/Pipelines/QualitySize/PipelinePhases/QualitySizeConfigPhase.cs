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
}
