using FluentValidation;
using FluentValidation.Results;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.Plan.Components;

internal class CustomFormatPlanner(
    ConfiguredCustomFormatProvider cfProvider,
    CustomFormatResourceQuery cfQuery,
    IValidator<CustomFormatGroupConfig> cfGroupValidator,
    IServiceConfiguration config,
    ILogger log
) : IPipelinePlanner<PlannedCustomFormats>
{
    public PlannedCustomFormats Plan(PlanningContext context)
    {
        // Validate explicit CF group config before resolution
        foreach (var groupConfig in config.CustomFormatGroups.Add)
        {
            var validationResult = cfGroupValidator.Validate(groupConfig);
            validationResult.ForwardTo(context.Add, log);
            foreach (var failure in validationResult.Errors)
            {
                context.AddPlanningOutcome(GetPlanningOutcome(failure));
            }
        }

        var cfResources = cfQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        // Group by TrashId (same CF can appear in multiple configs)
        var configuredCfs = cfProvider
            .GetAll(context.Add, context.AddPlanningOutcome)
            .GroupBy(x => x.TrashId, StringComparer.OrdinalIgnoreCase);

        var planned = new List<PlannedCustomFormat>();
        foreach (var group in configuredCfs)
        {
            if (!cfResources.TryGetValue(group.Key, out var resource))
            {
                context.Add(new InvalidCustomFormatTrashIdOutcome(group.Key));
                continue;
            }

            var first = group.First();
            planned.Add(
                new PlannedCustomFormat(resource)
                {
                    AssignScoresTo = group.SelectMany(x => x.AssignScoresTo).ToList(),
                    GroupName = first.GroupName,
                    Source = first.Source,
                    InclusionReason = first.InclusionReason,
                }
            );
        }

        return new PlannedCustomFormats(planned);
    }

    private static PlanningOutcome GetPlanningOutcome(ValidationFailure failure)
    {
        return failure.CustomState as PlanningOutcome
            ?? throw new InvalidOperationException(
                $"CF group validation did not produce a planning outcome: {failure.PropertyName}"
            );
    }
}
