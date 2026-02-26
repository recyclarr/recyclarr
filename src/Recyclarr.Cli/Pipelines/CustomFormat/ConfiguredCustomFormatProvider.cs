using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class ConfiguredCustomFormatProvider(
    IServiceConfiguration config,
    QualityProfileResourceQuery qpQuery,
    CfGroupResourceQuery cfGroupQuery,
    ILogger log
)
{
    public IEnumerable<ConfiguredCfEntry> GetAll(IDiagnosticPublisher diagnostics)
    {
        var qpResources = qpQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        var cfGroupResources = cfGroupQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        var skipSet = config.CustomFormatGroups.Skip.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // From flat custom_formats
        foreach (var cfg in config.CustomFormats)
        {
            var resolvedScores = ResolveAssignScoresTo(
                cfg.AssignScoresTo,
                qpResources,
                diagnostics
            );
            foreach (var trashId in cfg.TrashIds)
            {
                yield return new ConfiguredCfEntry(trashId, resolvedScores, null);
            }
        }

        // From QP formatItems
        foreach (var entry in FromFormatItems(qpResources))
        {
            yield return entry;
        }

        // From default CF groups (auto-discovered)
        foreach (var entry in FromDefaultGroups(qpResources, cfGroupResources, skipSet))
        {
            yield return entry;
        }

        // From explicit CF groups (assumes validation already ran)
        foreach (var entry in FromExplicitGroups(qpResources, cfGroupResources, diagnostics))
        {
            yield return entry;
        }
    }

    private IEnumerable<ConfiguredCfEntry> FromFormatItems(
        Dictionary<string, QualityProfileResource> qpResources
    )
    {
        var qpWithFormatItems = config
            .QualityProfiles.Where(qp => qp.TrashId is not null)
            .Select(qp => (Config: qp, Resource: qpResources.GetValueOrDefault(qp.TrashId!)))
            .Where(x => x.Resource?.FormatItems.Count > 0);

        foreach (var (qpConfig, qpResource) in qpWithFormatItems)
        {
            var assignScoresTo = new List<AssignScoresToConfig>
            {
                new()
                {
                    Name = !string.IsNullOrEmpty(qpConfig.Name) ? qpConfig.Name : qpResource!.Name,
                },
            };

            foreach (var trashId in qpResource!.FormatItems.Values)
            {
                yield return new ConfiguredCfEntry(
                    trashId,
                    assignScoresTo,
                    null,
                    CfSource.ProfileFormatItems
                );
            }
        }
    }

    // Auto-discovers default CF groups that match user's guide-backed quality profiles.
    // A group is auto-synced when:
    // 1. The group has Default == "true"
    // 2. At least one of the user's guide-backed QPs is in the group's include list
    // 3. The group is not in the skip list
    private IEnumerable<ConfiguredCfEntry> FromDefaultGroups(
        Dictionary<string, QualityProfileResource> qpResources,
        Dictionary<string, CfGroupResource> cfGroupResources,
        HashSet<string> skipSet
    )
    {
        // Check if user has any guide-backed profiles at all
        var hasGuideBacked = config.QualityProfiles.Any(qp =>
            qp.TrashId is not null && qpResources.ContainsKey(qp.TrashId)
        );

        if (!hasGuideBacked)
        {
            yield break;
        }

        // Explicitly added groups should not be auto-synced
        var explicitlyAddedGroups = config
            .CustomFormatGroups.Add.Select(g => g.TrashId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var groupResource in cfGroupResources.Values)
        {
            // Only process default groups
            if (!groupResource.Default.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Skip if user opted out
            if (skipSet.Contains(groupResource.TrashId))
            {
                log.Debug(
                    "Skipping default CF group {TrashId} (in skip list)",
                    groupResource.TrashId
                );
                continue;
            }

            // Skip if explicitly added (will be processed via FromExplicitGroups)
            if (explicitlyAddedGroups.Contains(groupResource.TrashId))
            {
                continue;
            }

            var includedTrashIds = groupResource.QualityProfiles.Include.Values.ToHashSet(
                StringComparer.OrdinalIgnoreCase
            );

            // Resolve all config profiles whose trash_id is in the include list
            var assignScoresTo = ResolveImplicitProfiles(includedTrashIds, qpResources);

            if (assignScoresTo.Count == 0)
            {
                continue;
            }

            log.Debug(
                "Auto-syncing default CF group {GroupName} for profiles: {Profiles}",
                groupResource.Name,
                string.Join(", ", assignScoresTo.Select(a => a.Name))
            );

            // Yield CFs with implicit source and appropriate inclusion reason
            foreach (var cf in groupResource.CustomFormats)
            {
                var reason =
                    cf.Required ? CfInclusionReason.Required
                    : cf.Default ? CfInclusionReason.Default
                    : CfInclusionReason.None;

                // Only include required and default CFs for implicit groups
                if (reason == CfInclusionReason.None)
                {
                    continue;
                }

                yield return new ConfiguredCfEntry(
                    cf.TrashId,
                    assignScoresTo,
                    groupResource.Name,
                    CfSource.CfGroupImplicit,
                    reason
                );
            }
        }
    }

    // Processes explicitly added CF groups (custom_format_groups.add).
    // Assumes validation already ran via ExplicitCfGroupValidator.
    private IEnumerable<ConfiguredCfEntry> FromExplicitGroups(
        Dictionary<string, QualityProfileResource> qpResources,
        Dictionary<string, CfGroupResource> cfGroupResources,
        IDiagnosticPublisher diagnostics
    )
    {
        foreach (var groupConfig in config.CustomFormatGroups.Add)
        {
            if (!cfGroupResources.TryGetValue(groupConfig.TrashId, out var groupResource))
            {
                continue;
            }

            var assignScoresTo = DetermineProfiles(
                groupConfig,
                groupResource,
                qpResources,
                diagnostics
            );

            if (assignScoresTo.Count == 0)
            {
                log.Debug("CF group {TrashId} has no profiles to assign to", groupConfig.TrashId);
                continue;
            }

            var cfEntries = ResolveCfsForGroup(groupConfig, groupResource);

            var hasEntries = false;
            foreach (var (trashId, reason) in cfEntries)
            {
                hasEntries = true;
                yield return new ConfiguredCfEntry(
                    trashId,
                    assignScoresTo,
                    groupResource.Name,
                    CfSource.CfGroupExplicit,
                    reason
                );
            }

            if (!hasEntries)
            {
                diagnostics.AddWarning(
                    $"CF group '{groupResource.Name}' ({groupConfig.TrashId}) has no custom formats after"
                        + " applying opt-in semantics. All CFs in this group are optional; use `select`"
                        + " to pick specific CFs to include."
                );
            }
        }
    }

    // Resolves which CFs to include via composition: (required) + (defaults - exclude) + (select)
    private static IEnumerable<(string TrashId, CfInclusionReason Reason)> ResolveCfsForGroup(
        CustomFormatGroupConfig groupConfig,
        CfGroupResource groupResource
    )
    {
        var selectSet = groupConfig.Select.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var excludeSet = groupConfig.Exclude.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var cf in groupResource.CustomFormats)
        {
            if (cf.Required)
            {
                yield return (cf.TrashId, CfInclusionReason.Required);
                continue;
            }

            if (cf.Default)
            {
                if (!excludeSet.Contains(cf.TrashId))
                {
                    yield return (cf.TrashId, CfInclusionReason.Default);
                }

                continue;
            }

            // Non-default, non-required: include if select_all or explicitly selected
            if (groupConfig.SelectAll || selectSet.Contains(cf.TrashId))
            {
                if (!excludeSet.Contains(cf.TrashId))
                {
                    yield return (cf.TrashId, CfInclusionReason.Selected);
                }
            }
        }
    }

    // Resolves flat custom_formats assign_scores_to entries using shared reference resolution.
    private List<AssignScoresToConfig> ResolveAssignScoresTo(
        ICollection<AssignScoresToConfig> scores,
        Dictionary<string, QualityProfileResource> qpResources,
        IDiagnosticPublisher diagnostics
    )
    {
        return scores
            .SelectMany(s => ResolveProfileReference(s, qpResources, diagnostics, "custom_formats"))
            .ToList();
    }

    // Returns the list of profiles to assign this group's CFs to.
    private List<AssignScoresToConfig> DetermineProfiles(
        CustomFormatGroupConfig groupConfig,
        CfGroupResource groupResource,
        Dictionary<string, QualityProfileResource> qpResources,
        IDiagnosticPublisher diagnostics
    )
    {
        if (groupConfig.AssignScoresTo.Count > 0)
        {
            return groupConfig
                .AssignScoresTo.SelectMany(entry =>
                    ResolveProfileReference(
                        entry,
                        qpResources,
                        diagnostics,
                        $"CF group '{groupConfig.TrashId}'"
                    )
                )
                .ToList();
        }

        // Implicit: all config profiles whose trash_id is in the group's include list
        var includedTrashIds = new HashSet<string>(
            groupResource.QualityProfiles.Include.Values,
            StringComparer.OrdinalIgnoreCase
        );

        return ResolveImplicitProfiles(includedTrashIds, qpResources);
    }

    // Resolves all config profiles whose trash_id is in the given include set.
    // Returns one entry per config profile (including variants with the same trash_id).
    private List<AssignScoresToConfig> ResolveImplicitProfiles(
        HashSet<string> includedTrashIds,
        Dictionary<string, QualityProfileResource> qpResources
    )
    {
        return config
            .QualityProfiles.Where(qp => qp.TrashId is not null)
            .Where(qp => qpResources.ContainsKey(qp.TrashId!))
            .Where(qp => includedTrashIds.Contains(qp.TrashId!))
            .Select(qp => new AssignScoresToConfig
            {
                TrashId = qp.TrashId,
                // non-null: filtered to profiles with trash_id in guide resources above
                Name = !string.IsNullOrEmpty(qp.Name) ? qp.Name : qpResources[qp.TrashId!].Name,
            })
            .ToList();
    }

    // Resolves a single profile reference (trash_id XOR name) to concrete profile entries.
    // For name: returns entry as-is.
    // For trash_id: resolves to the config profile if unambiguous (1 match), or errors if 2+.
    private IEnumerable<AssignScoresToConfig> ResolveProfileReference(
        AssignScoresToConfig reference,
        Dictionary<string, QualityProfileResource> qpResources,
        IDiagnosticPublisher diagnostics,
        string context
    )
    {
        if (!string.IsNullOrEmpty(reference.Name))
        {
            return [reference];
        }

        if (string.IsNullOrEmpty(reference.TrashId))
        {
            return [];
        }

        // Find all config profiles with this trash_id
        var matchingProfiles = config
            .QualityProfiles.Where(qp =>
                reference.TrashId.Equals(qp.TrashId, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        if (matchingProfiles.Count > 1)
        {
            var names = matchingProfiles.Select(qp => $"'{qp.Name}'");
            diagnostics.AddError(
                $"[{context}] trash_id '{reference.TrashId}' matches multiple profiles: "
                    + $"{string.Join(", ", names)}. Use 'name' to specify which profile to target."
            );
            return [];
        }

        // Single match or no config match (fall back to guide name)
        var configQp = matchingProfiles.FirstOrDefault();
        var name =
            configQp is not null && !string.IsNullOrEmpty(configQp.Name)
                ? configQp.Name
                : qpResources.GetValueOrDefault(reference.TrashId)?.Name ?? "";

        return [reference with { Name = name }];
    }
}
