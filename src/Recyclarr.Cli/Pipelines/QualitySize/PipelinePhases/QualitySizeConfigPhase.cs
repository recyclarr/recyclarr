using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeConfigPhase(ILogger log, IQualitySizeGuideService guide, IServiceConfiguration config)
    : IConfigPipelinePhase<QualitySizePipelineContext>
{
    public Task Execute(QualitySizePipelineContext context)
    {
        var qualityDef = config.QualityDefinition;
        if (qualityDef is null)
        {
            log.Debug("{Instance} has no quality definition", config.InstanceName);
            return Task.CompletedTask;
        }

        var qualityDefinitions = guide.GetQualitySizeData(config.ServiceType);
        var selectedQuality = qualityDefinitions
            .FirstOrDefault(x => x.Type.EqualsIgnoreCase(qualityDef.Type));

        if (selectedQuality == null)
        {
            context.ConfigError = $"The specified quality definition type does not exist: {qualityDef.Type}";
            return Task.CompletedTask;
        }

        AdjustPreferredRatio(qualityDef, selectedQuality);
        context.ConfigOutput = selectedQuality;
        return Task.CompletedTask;
    }

    private void AdjustPreferredRatio(QualityDefinitionConfig qualityDefConfig, QualitySizeData selectedQuality)
    {
        if (qualityDefConfig.PreferredRatio is null)
        {
            return;
        }

        log.Information("Using an explicit preferred ratio which will override values from the guide");

        // Fix an out of range ratio and warn the user
        if (qualityDefConfig.PreferredRatio is < 0 or > 1)
        {
            var clampedRatio = Math.Clamp(qualityDefConfig.PreferredRatio.Value, 0, 1);
            log.Warning("Your `preferred_ratio` of {CurrentRatio} is out of range. " +
                "It must be a decimal between 0.0 and 1.0. It has been clamped to {ClampedRatio}",
                qualityDefConfig.PreferredRatio, clampedRatio);

            qualityDefConfig.PreferredRatio = clampedRatio;
        }

        // Apply a calculated preferred size
        foreach (var quality in selectedQuality.Qualities)
        {
            quality.Preferred = quality.InterpolatedPreferred(qualityDefConfig.PreferredRatio.Value);
        }
    }
}
