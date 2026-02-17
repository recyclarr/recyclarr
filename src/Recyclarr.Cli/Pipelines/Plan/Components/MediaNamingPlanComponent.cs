using Autofac.Features.Indexed;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.Sync;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class MediaNamingPlanComponent(
    IInstancePublisher events,
    MediaNamingResourceQuery guide,
    IIndex<SupportedServices, IServiceBasedMediaNamingConfigPhase> configPhaseStrategyFactory,
    IServiceConfiguration config
) : IPlanComponent
{
    public void Process(PipelinePlan plan)
    {
        var lookup = new NamingFormatLookup();
        var strategy = configPhaseStrategyFactory[config.ServiceType];
        var dto = strategy.ProcessNaming(guide, lookup);

        // Capture validation errors in diagnostics
        foreach (var (type, configValue) in lookup.Errors)
        {
            events.AddError($"Invalid {type} naming format: {configValue}");
        }

        // Check if there are any differences from defaults
        var differences = dto switch
        {
            RadarrMediaNamingDto x => x.GetDifferences(new RadarrMediaNamingDto()),
            SonarrMediaNamingDto x => x.GetDifferences(new SonarrMediaNamingDto()),
            _ => throw new InvalidOperationException("Unsupported media naming DTO type"),
        };

        if (differences.Count == 0)
        {
            // No changes to process - don't add to plan
            return;
        }

        plan.MediaNaming = new PlannedMediaNaming { Dto = dto };
    }
}
