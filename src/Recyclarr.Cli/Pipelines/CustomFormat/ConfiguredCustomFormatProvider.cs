using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync.Events;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class ConfiguredCustomFormatProvider(
    IServiceConfiguration config,
    QualityProfileResourceQuery qpQuery,
    CfGroupResourceQuery cfGroupQuery,
    ISyncEventPublisher events,
    ILogger log
)
{
    public IEnumerable<ConfiguredCfEntry> GetAll()
    {
        var qpResources = qpQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        // From flat custom_formats (GroupName = null)
        foreach (var cfg in config.CustomFormats)
        {
            foreach (var trashId in cfg.TrashIds)
            {
                yield return new ConfiguredCfEntry(trashId, cfg.AssignScoresTo, null);
            }
        }

        // From QP formatItems (GroupName = null)
        foreach (var entry in FromFormatItems(qpResources))
        {
            yield return entry;
        }

        // From CF groups (GroupName = group name)
        foreach (var entry in FromCfGroups(qpResources))
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
                yield return new ConfiguredCfEntry(trashId, assignScoresTo, null);
            }
        }
    }

    // Resolves CF group configs to ConfiguredCfEntry entries by looking up guide resources,
    // filtering CFs by exclude list, and determining which profiles to assign scores to.
    private IEnumerable<ConfiguredCfEntry> FromCfGroups(
        Dictionary<string, QualityProfileResource> qpResources
    )
    {
        var cfGroupResources = cfGroupQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        foreach (var groupConfig in config.CustomFormatGroups)
        {
            // Resolve group trash_id to guide resource
            if (!cfGroupResources.TryGetValue(groupConfig.TrashId, out var groupResource))
            {
                events.AddError($"Invalid custom format group trash_id: {groupConfig.TrashId}");
                continue;
            }

            // Validate exclude list
            if (!ValidateExcludeList(groupConfig, groupResource))
            {
                continue;
            }

            // Determine which profiles this group's CFs should be assigned to
            var assignScoresTo = DetermineProfiles(groupConfig, groupResource, qpResources);
            if (assignScoresTo is null)
            {
                // Validation errors occurred, skip this group
                continue;
            }

            if (assignScoresTo.Count == 0)
            {
                log.Debug("CF group {TrashId} has no profiles to assign to", groupConfig.TrashId);
                continue;
            }

            // Filter out excluded CFs and yield individual entries
            var cfTrashIds = groupResource
                .CustomFormats.Where(cf =>
                    !groupConfig.Exclude.Contains(cf.TrashId, StringComparer.OrdinalIgnoreCase)
                )
                .Select(cf => cf.TrashId);

            var hasEntries = false;
            foreach (var trashId in cfTrashIds)
            {
                hasEntries = true;
                yield return new ConfiguredCfEntry(trashId, assignScoresTo, groupResource.Name);
            }

            if (!hasEntries)
            {
                log.Debug("CF group {TrashId} has no CFs after exclusions", groupConfig.TrashId);
            }
        }
    }

    // Validates the exclude list for a CF group. Returns true if valid, false if errors found.
    private bool ValidateExcludeList(
        CustomFormatGroupConfig groupConfig,
        CfGroupResource groupResource
    )
    {
        var groupCfTrashIds = groupResource
            .CustomFormats.Select(cf => cf.TrashId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hasErrors = false;

        foreach (var excludeId in groupConfig.Exclude)
        {
            // Check if the excluded trash_id exists in this group
            if (!groupCfTrashIds.Contains(excludeId))
            {
                events.AddError(
                    $"CF group '{groupConfig.TrashId}': Invalid CF trash_id in exclude: {excludeId}"
                );
                hasErrors = true;
                continue;
            }

            // Check if the excluded CF is marked as required
            var cf = groupResource.CustomFormats.First(c =>
                c.TrashId.Equals(excludeId, StringComparison.OrdinalIgnoreCase)
            );
            if (cf.Required)
            {
                events.AddError(
                    $"CF group '{groupConfig.TrashId}': Cannot exclude required CF '{excludeId}'"
                );
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    // Returns the list of profiles to assign this group's CFs to. If the user specified
    // explicit assign_scores_to entries, validates and uses those. Otherwise, uses
    // guide-backed quality profiles from the config that are in the group's include list.
    // Returns null if validation errors occurred for explicit profiles.
    private List<AssignScoresToConfig>? DetermineProfiles(
        CustomFormatGroupConfig groupConfig,
        CfGroupResource groupResource,
        Dictionary<string, QualityProfileResource> qpResources
    )
    {
        // Build set of profile trash_ids included by the guide for this group
        var includedProfiles = new HashSet<string>(
            groupResource.QualityProfiles.Include.Values,
            StringComparer.OrdinalIgnoreCase
        );

        if (groupConfig.AssignScoresTo.Count > 0)
        {
            // Explicit: user specified profiles - validate each one
            return ValidateExplicitProfiles(groupConfig, includedProfiles, qpResources);
        }

        // Implicit: guide-backed profiles in user's config that are in the include list
        // Use config name if set, otherwise fall back to guide resource name
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

    // Validates explicit assign_scores_to profiles. Returns null if errors found.
    private List<AssignScoresToConfig>? ValidateExplicitProfiles(
        CustomFormatGroupConfig groupConfig,
        HashSet<string> includedProfiles,
        Dictionary<string, QualityProfileResource> qpResources
    )
    {
        var hasErrors = false;
        var result = new List<AssignScoresToConfig>();

        foreach (var score in groupConfig.AssignScoresTo)
        {
            // Check if profile trash_id exists
            if (!qpResources.TryGetValue(score.TrashId, out var qpResource))
            {
                events.AddError(
                    $"CF group '{groupConfig.TrashId}': Invalid profile trash_id in assign_scores_to: {score.TrashId}"
                );
                hasErrors = true;
                continue;
            }

            // Check if profile is in the guide's include list
            if (!includedProfiles.Contains(score.TrashId))
            {
                events.AddError(
                    $"CF group '{groupConfig.TrashId}': Profile '{score.TrashId}' is not in this group's include list"
                );
                hasErrors = true;
                continue;
            }

            result.Add(
                new AssignScoresToConfig { TrashId = score.TrashId, Name = qpResource.Name }
            );
        }

        return hasErrors ? null : result;
    }
}
