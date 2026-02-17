using FluentValidation;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class ExplicitCfGroupValidator : AbstractValidator<CustomFormatGroupConfig>
{
    public ExplicitCfGroupValidator(
        CfGroupResourceQuery cfGroupQuery,
        QualityProfileResourceQuery qpQuery,
        IServiceConfiguration config
    )
    {
        var groups = cfGroupQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        var profiles = qpQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        RuleFor(g => g.TrashId)
            .Must(id => groups.ContainsKey(id))
            .WithMessage(g => $"Invalid custom format group trash_id: {g.TrashId}");

        When(
            g => groups.ContainsKey(g.TrashId),
            () =>
            {
                RuleForEach(g => g.Select)
                    .Must(
                        (g, selectId) =>
                            groups[g.TrashId]
                                .CustomFormats.Any(cf => cf.TrashId.EqualsIgnoreCase(selectId))
                    )
                    .WithMessage(
                        (g, selectId) =>
                            $"CF group '{g.TrashId}': Invalid CF trash_id in select: {selectId}"
                    );

                RuleForEach(g => g.Select)
                    .Must(
                        (g, selectId) =>
                        {
                            var cf = groups[g.TrashId]
                                .CustomFormats.FirstOrDefault(cf =>
                                    cf.TrashId.EqualsIgnoreCase(selectId)
                                );
                            return cf is not { Required: true };
                        }
                    )
                    .WithSeverity(Severity.Warning)
                    .WithMessage(
                        (g, selectId) =>
                            $"CF group '{g.TrashId}': Selecting required CF '{selectId}' is redundant "
                            + "(required CFs are always included)"
                    );

                RuleForEach(g => g.AssignScoresTo)
                    .Must(s => profiles.ContainsKey(s.TrashId))
                    .WithMessage(
                        (g, s) =>
                            $"CF group '{g.TrashId}': Invalid profile trash_id in assign_scores_to: {s.TrashId}"
                    );

                RuleForEach(g => g.AssignScoresTo)
                    .Must(
                        (g, s) =>
                            !profiles.ContainsKey(s.TrashId)
                            || groups[g.TrashId]
                                .QualityProfiles.Include.Values.Any(v =>
                                    v.Equals(s.TrashId, StringComparison.OrdinalIgnoreCase)
                                )
                    )
                    .WithMessage(
                        (g, s) =>
                            $"CF group '{g.TrashId}': Profile '{s.TrashId}' is not in this group's include list"
                    );
            }
        );
    }
}
