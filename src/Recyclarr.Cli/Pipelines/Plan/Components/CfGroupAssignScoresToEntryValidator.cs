using FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class CfGroupAssignScoresToEntryValidator : AbstractValidator<AssignScoresToConfig>
{
    public CfGroupAssignScoresToEntryValidator(
        string groupTrashId,
        IReadOnlyDictionary<string, QualityProfileResource> guideProfiles
    )
    {
        RuleFor(s => s.TrashId)
            .Must(id => guideProfiles.ContainsKey(id!))
            .When(s => !string.IsNullOrEmpty(s.TrashId))
            .WithMessage(s =>
                $"CF group '{groupTrashId}': Invalid profile trash_id "
                + $"in assign_scores_to: {s.TrashId}"
            );
    }
}
