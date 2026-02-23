using FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class CfGroupAssignScoresToEntryValidator : AbstractValidator<CfGroupAssignScoresToConfig>
{
    public CfGroupAssignScoresToEntryValidator(
        string groupTrashId,
        IReadOnlyDictionary<string, QualityProfileResource> guideProfiles
    )
    {
        var guideProfileNames = guideProfiles
            .Values.Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        RuleFor(s => s.TrashId)
            .Must(id => guideProfiles.ContainsKey(id!))
            .When(s => !string.IsNullOrEmpty(s.TrashId))
            .WithMessage(s =>
                $"CF group '{groupTrashId}': Invalid profile trash_id "
                + $"in assign_scores_to: {s.TrashId}"
            );

        RuleFor(s => s.Name)
            .Must(name => !guideProfileNames.Contains(name!))
            .When(s => !string.IsNullOrEmpty(s.Name))
            .WithMessage(s =>
                $"CF group '{groupTrashId}': Profile '{s.Name}' is guide-backed; "
                + "use trash_id instead of name to reference it"
            );
    }
}
