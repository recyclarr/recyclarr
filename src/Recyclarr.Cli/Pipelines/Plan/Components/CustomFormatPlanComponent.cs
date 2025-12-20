using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync.Events;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class CustomFormatPlanComponent(
    ConfiguredCustomFormatProvider cfProvider,
    CustomFormatResourceQuery cfQuery,
    IServiceConfiguration config
) : IPlanComponent
{
    public void Process(PipelinePlan plan, ISyncEventPublisher events)
    {
        var cfResources = GetCfResourcesForService()
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        // Flatten configs into (TrashId, AssignScoresTo) pairs, then group by TrashId
        var configuredCfs = cfProvider
            .GetAll()
            .SelectMany(cfg => cfg.TrashIds.Select(id => (TrashId: id, cfg.AssignScoresTo)))
            .GroupBy(x => x.TrashId, StringComparer.OrdinalIgnoreCase);

        foreach (var group in configuredCfs)
        {
            if (!cfResources.TryGetValue(group.Key, out var resource))
            {
                events.AddWarning($"Invalid trash_id: {group.Key}");
                continue;
            }

            plan.AddCustomFormat(
                new PlannedCustomFormat(resource)
                {
                    AssignScoresTo = group.SelectMany(x => x.AssignScoresTo).ToList(),
                }
            );
        }
    }

    private IReadOnlyList<CustomFormatResource> GetCfResourcesForService()
    {
        return config.ServiceType switch
        {
            SupportedServices.Radarr => cfQuery.GetRadarr(),
            SupportedServices.Sonarr => cfQuery.GetSonarr(),
            _ => throw new InvalidOperationException($"Unknown service type: {config.ServiceType}"),
        };
    }
}
