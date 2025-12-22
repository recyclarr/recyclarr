using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync.Events;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class CustomFormatPlanComponent(
    ConfiguredCustomFormatProvider cfProvider,
    CustomFormatResourceQuery cfQuery,
    IServiceConfiguration config
) : IPlanComponent
{
    public void Process(PipelinePlan plan, ISyncEventPublisher events)
    {
        var cfResources = cfQuery
            .Get(config.ServiceType)
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
}
