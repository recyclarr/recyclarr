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

        // Group by TrashId (same CF can appear in multiple configs)
        var configuredCfs = cfProvider
            .GetAll()
            .GroupBy(x => x.TrashId, StringComparer.OrdinalIgnoreCase);

        foreach (var group in configuredCfs)
        {
            if (!cfResources.TryGetValue(group.Key, out var resource))
            {
                events.AddWarning($"Invalid trash_id: {group.Key}");
                continue;
            }

            // Use the first entry's source info (same CF may appear multiple times,
            // but the source from the first occurrence is representative)
            var first = group.First();
            plan.AddCustomFormat(
                new PlannedCustomFormat(resource)
                {
                    AssignScoresTo = group.SelectMany(x => x.AssignScoresTo).ToList(),
                    GroupName = first.GroupName,
                    Source = first.Source,
                    InclusionReason = first.InclusionReason,
                }
            );
        }
    }
}
