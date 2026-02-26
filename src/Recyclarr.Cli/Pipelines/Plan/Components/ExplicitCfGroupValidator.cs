using FluentValidation;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class ExplicitCfGroupValidator : AbstractValidator<CustomFormatGroupConfig>
{
    private readonly Dictionary<string, CfGroupResource> _groups;

    public ExplicitCfGroupValidator(
        CfGroupResourceQuery cfGroupQuery,
        QualityProfileResourceQuery qpQuery,
        IServiceConfiguration config
    )
    {
        _groups = cfGroupQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        var profiles = qpQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        RuleFor(g => g.TrashId)
            .Must(id => _groups.ContainsKey(id))
            .WithMessage(g => $"Invalid custom format group trash_id: {g.TrashId}");

        When(
            g => _groups.ContainsKey(g.TrashId),
            () =>
            {
                AddSelectRules();
                AddExcludeRules();

                RuleForEach(g => g.AssignScoresTo)
                    .SetValidator(g => new CfGroupAssignScoresToEntryValidator(
                        g.TrashId,
                        profiles
                    ));
            }
        );
    }

    private CfGroupCustomFormat? FindCf(CustomFormatGroupConfig g, string cfId) =>
        _groups[g.TrashId].CustomFormats.FirstOrDefault(cf => cf.TrashId.EqualsIgnoreCase(cfId));

    private bool CfExistsInGroup(CustomFormatGroupConfig g, string cfId) =>
        _groups[g.TrashId].CustomFormats.Any(cf => cf.TrashId.EqualsIgnoreCase(cfId));

    private void AddSelectRules()
    {
        // select_all bypasses all select validation (no select list to validate)
        RuleForEach(g => g.Select)
            .Must((g, selectId) => g.SelectAll || CfExistsInGroup(g, selectId))
            .WithMessage(
                (g, selectId) =>
                    $"CF group '{g.TrashId}': Invalid CF trash_id in select: {selectId}"
            );

        RuleForEach(g => g.Select)
            .Must((g, selectId) => g.SelectAll || FindCf(g, selectId) is not { Required: true })
            .WithSeverity(Severity.Warning)
            .WithMessage(
                (g, selectId) =>
                    $"CF group '{g.TrashId}': Selecting required CF '{selectId}' is redundant "
                    + "(required CFs are always included)"
            );

        RuleForEach(g => g.Select)
            .Must((g, selectId) => g.SelectAll || FindCf(g, selectId) is not { Default: true })
            .WithSeverity(Severity.Warning)
            .WithMessage(
                (g, selectId) =>
                    $"CF group '{g.TrashId}': Selecting default CF '{selectId}' is redundant "
                    + "(default CFs are already included)"
            );
    }

    private void AddExcludeRules()
    {
        RuleForEach(g => g.Exclude)
            .Must((g, excludeId) => CfExistsInGroup(g, excludeId))
            .WithMessage(
                (g, excludeId) =>
                    $"CF group '{g.TrashId}': Invalid CF trash_id in exclude: {excludeId}"
            );

        RuleForEach(g => g.Exclude)
            .Must((g, excludeId) => FindCf(g, excludeId) is not { Required: true })
            .WithSeverity(Severity.Warning)
            .WithMessage(
                (g, excludeId) =>
                    $"CF group '{g.TrashId}': Excluding required CF '{excludeId}' has no effect "
                    + "(required CFs are always included)"
            );

        // With select_all, excluding any non-required CF is valid
        RuleForEach(g => g.Exclude)
            .Must(
                (g, excludeId) =>
                    g.SelectAll
                    || FindCf(g, excludeId) is null or { Default: true } or { Required: true }
            )
            .WithSeverity(Severity.Warning)
            .WithMessage(
                (g, excludeId) =>
                    $"CF group '{g.TrashId}': Excluding non-default CF '{excludeId}' has no effect "
                    + "(only default CFs can be excluded)"
            );
    }
}
