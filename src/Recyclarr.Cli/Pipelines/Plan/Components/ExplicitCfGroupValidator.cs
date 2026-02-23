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

                // Warn if selecting a default CF (redundant; already included via defaults)
                RuleForEach(g => g.Select)
                    .Must(
                        (g, selectId) =>
                        {
                            var cf = groups[g.TrashId]
                                .CustomFormats.FirstOrDefault(cf =>
                                    cf.TrashId.EqualsIgnoreCase(selectId)
                                );
                            return cf is not { Default: true };
                        }
                    )
                    .WithSeverity(Severity.Warning)
                    .WithMessage(
                        (g, selectId) =>
                            $"CF group '{g.TrashId}': Selecting default CF '{selectId}' is redundant "
                            + "(default CFs are already included)"
                    );

                // Validate exclude trash_ids exist in group
                RuleForEach(g => g.Exclude)
                    .Must(
                        (g, excludeId) =>
                            groups[g.TrashId]
                                .CustomFormats.Any(cf => cf.TrashId.EqualsIgnoreCase(excludeId))
                    )
                    .WithMessage(
                        (g, excludeId) =>
                            $"CF group '{g.TrashId}': Invalid CF trash_id in exclude: {excludeId}"
                    );

                // Warn if excluding a required CF (it will still be included)
                RuleForEach(g => g.Exclude)
                    .Must(
                        (g, excludeId) =>
                        {
                            var cf = groups[g.TrashId]
                                .CustomFormats.FirstOrDefault(cf =>
                                    cf.TrashId.EqualsIgnoreCase(excludeId)
                                );
                            return cf is not { Required: true };
                        }
                    )
                    .WithSeverity(Severity.Warning)
                    .WithMessage(
                        (g, excludeId) =>
                            $"CF group '{g.TrashId}': Excluding required CF '{excludeId}' has no effect "
                            + "(required CFs are always included)"
                    );

                // Warn if excluding a non-default CF (no-op; wasn't included anyway)
                RuleForEach(g => g.Exclude)
                    .Must(
                        (g, excludeId) =>
                        {
                            var cf = groups[g.TrashId]
                                .CustomFormats.FirstOrDefault(cf =>
                                    cf.TrashId.EqualsIgnoreCase(excludeId)
                                );
                            return cf is null or { Default: true } or { Required: true };
                        }
                    )
                    .WithSeverity(Severity.Warning)
                    .WithMessage(
                        (g, excludeId) =>
                            $"CF group '{g.TrashId}': Excluding non-default CF '{excludeId}' has no effect "
                            + "(only default CFs can be excluded)"
                    );

                RuleForEach(g => g.AssignScoresTo)
                    .SetValidator(g => new CfGroupAssignScoresToEntryValidator(
                        g.TrashId,
                        profiles
                    ));
            }
        );
    }
}
