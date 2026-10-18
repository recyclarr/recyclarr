using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.Pipelines.Plan.Components;

/// <summary>
/// Plans instance-wide quality sizes on services that store sizes globally. On services that
/// store sizes per quality profile (Sonarr v5) it plans nothing; the quality profile planner
/// assigns sizes there.
/// </summary>
internal class QualitySizePlanComponent(
    QualitySizeResolver resolver,
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
            return;
        }

        var configSizeData = config.QualityDefinition;
        if (configSizeData is null)
        {
            log.Debug("No quality_definition configured, skipping quality sizes");
            return;
        }

        plan.QualitySizes = resolver.Resolve(configSizeData, plan.Add);
    }
}
