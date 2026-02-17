using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class ConfiguredCustomFormatProvider(
    IServiceConfiguration config,
    QualityProfileResourceQuery qpQuery,
    CfGroupResourceQuery cfGroupQuery,
    ILogger log
)
{
    public IEnumerable<ConfiguredCfEntry> GetAll()
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
            foreach (var trashId in cfg.TrashIds)
            {
                yield return new ConfiguredCfEntry(trashId, cfg.AssignScoresTo, null);
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
        foreach (var entry in FromExplicitGroups(qpResources, cfGroupResources))
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
        // Collect guide-backed quality profile trash_ids from user's config
        var userQpTrashIds = config
            .QualityProfiles.Where(qp => qp.TrashId is not null)
            .Where(qp => qpResources.ContainsKey(qp.TrashId!))
            .Select(qp => qp.TrashId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (userQpTrashIds.Count == 0)
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

            // Check if any user QP is in this group's include list
            var includedProfiles = groupResource.QualityProfiles.Include.Values.ToHashSet(
                StringComparer.OrdinalIgnoreCase
            );
            var matchingQps = userQpTrashIds.Where(qp => includedProfiles.Contains(qp)).ToList();

            if (matchingQps.Count == 0)
            {
                continue;
            }

            // Build AssignScoresTo from matching profiles
            var assignScoresTo = matchingQps
                .Select(qpTrashId =>
                {
                    var qpConfig = config.QualityProfiles.First(q =>
                        q.TrashId?.Equals(qpTrashId, StringComparison.OrdinalIgnoreCase) == true
                    );
                    return new AssignScoresToConfig
                    {
                        TrashId = qpTrashId,
                        Name = !string.IsNullOrEmpty(qpConfig.Name)
                            ? qpConfig.Name
                            : qpResources[qpTrashId].Name,
                    };
                })
                .ToList();

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
        Dictionary<string, CfGroupResource> cfGroupResources
    )
    {
        foreach (var groupConfig in config.CustomFormatGroups.Add)
        {
            if (!cfGroupResources.TryGetValue(groupConfig.TrashId, out var groupResource))
            {
                continue;
            }

            var assignScoresTo = DetermineProfiles(groupConfig, groupResource, qpResources);

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
                log.Debug(
                    "CF group {TrashId} has no CFs after applying opt-in semantics",
                    groupConfig.TrashId
                );
            }
        }
    }

    // Resolves which CFs to include using opt-in semantics, returning inclusion reason:
    // - Always include required CFs (Required)
    // - If select is empty: include default CFs (Default)
    // - If select is non-empty: include only selected CFs (Selected)
    private static IEnumerable<(string TrashId, CfInclusionReason Reason)> ResolveCfsForGroup(
        CustomFormatGroupConfig groupConfig,
        CfGroupResource groupResource
    )
    {
        var selectedSet = groupConfig.Select.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasSelections = selectedSet.Count > 0;

        foreach (var cf in groupResource.CustomFormats)
        {
            // Required CFs are always included
            if (cf.Required)
            {
                yield return (cf.TrashId, CfInclusionReason.Required);
                continue;
            }

            // Optional CFs: check select list or fall back to defaults
            if (hasSelections)
            {
                // User specified selections - only include if selected
                if (selectedSet.Contains(cf.TrashId))
                {
                    yield return (cf.TrashId, CfInclusionReason.Selected);
                }
            }
            else
            {
                // No selections - include defaults
                if (cf.Default)
                {
                    yield return (cf.TrashId, CfInclusionReason.Default);
                }
            }
        }
    }

    // Returns the list of profiles to assign this group's CFs to.
    // Assumes validation already ran via ExplicitCfGroupValidator.
    private List<AssignScoresToConfig> DetermineProfiles(
        CustomFormatGroupConfig groupConfig,
        CfGroupResource groupResource,
        Dictionary<string, QualityProfileResource> qpResources
    )
    {
        var includedProfiles = new HashSet<string>(
            groupResource.QualityProfiles.Include.Values,
            StringComparer.OrdinalIgnoreCase
        );

        if (groupConfig.AssignScoresTo.Count > 0)
        {
            // Explicit: user specified profiles (already validated)
            return groupConfig
                .AssignScoresTo.Where(s =>
                    qpResources.ContainsKey(s.TrashId) && includedProfiles.Contains(s.TrashId)
                )
                .Select(s => new AssignScoresToConfig
                {
                    TrashId = s.TrashId,
                    Name = qpResources[s.TrashId].Name,
                })
                .ToList();
        }

        // Implicit: guide-backed profiles in user's config that are in the include list
        return config
            .QualityProfiles.Where(qp => qp.TrashId is not null)
            .Where(qp => qpResources.ContainsKey(qp.TrashId!))
            .Where(qp => includedProfiles.Contains(qp.TrashId!))
            .Select(qp => new AssignScoresToConfig
            {
                TrashId = qp.TrashId,
                Name = !string.IsNullOrEmpty(qp.Name) ? qp.Name : qpResources[qp.TrashId!].Name,
            })
            .ToList();
    }
}
