using FluentValidation;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Pipelines.Plan.Components;

internal class CustomFormatPlanComponent(
    ConfiguredCustomFormatProvider cfProvider,
    CustomFormatResourceQuery cfQuery,
    IValidator<CustomFormatGroupConfig> cfGroupValidator,
    IServiceConfiguration config,
    ILogger log
) : IPlanComponent
{
    public void Process(PipelinePlan plan)
    {
        // Validate explicit CF group config before resolution
        foreach (var groupConfig in config.CustomFormatGroups.Add)
        {
            cfGroupValidator.Validate(groupConfig).ForwardTo(plan, log);
        }

        var cfResources = cfQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        // Group by TrashId (same CF can appear in multiple configs)
        var configuredCfs = cfProvider
            .GetAll(plan)
            .GroupBy(x => x.TrashId, StringComparer.OrdinalIgnoreCase);

        foreach (var group in configuredCfs)
        {
            if (!cfResources.TryGetValue(group.Key, out var resource))
            {
                plan.AddWarning($"Invalid trash_id: {group.Key}");
                continue;
            }

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
