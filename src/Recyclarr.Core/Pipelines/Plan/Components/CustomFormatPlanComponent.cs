using FluentValidation;
using FluentValidation.Results;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync.Results;

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
            var validationResult = cfGroupValidator.Validate(groupConfig);
            validationResult.ForwardTo(plan, log);
            foreach (var failure in validationResult.Errors)
            {
                plan.AddPlanningOutcome(GetPlanningOutcome(failure));
            }
        }

        var cfResources = cfQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        // Group by TrashId (same CF can appear in multiple configs)
        var configuredCfs = cfProvider
            .GetAll(plan, plan.AddPlanningOutcome)
            .GroupBy(x => x.TrashId, StringComparer.OrdinalIgnoreCase);

        foreach (var group in configuredCfs)
        {
            if (!cfResources.TryGetValue(group.Key, out var resource))
            {
                plan.Add(new InvalidCustomFormatTrashIdOutcome(group.Key));
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

    private static PlanningOutcome GetPlanningOutcome(ValidationFailure failure)
    {
        return failure.CustomState as PlanningOutcome
            ?? throw new InvalidOperationException(
                $"CF group validation did not produce a planning outcome: {failure.PropertyName}"
            );
    }
}
