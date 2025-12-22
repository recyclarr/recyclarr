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
    public IEnumerable<CustomFormatConfig> GetAll()
    {
        var qpResources = qpQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        // Synthesize CustomFormatConfig from QP formatItems
        var fromFormatItems = config
            .QualityProfiles.Where(qp => qp.TrashId is not null)
            .Select(qp => (Config: qp, Resource: qpResources.GetValueOrDefault(qp.TrashId!)))
            .Where(x => x.Resource?.FormatItems.Count > 0)
            .Select(x => new CustomFormatConfig
            {
                TrashIds = x.Resource!.FormatItems.Values.ToList(),
                AssignScoresTo =
                [
                    new AssignScoresToConfig
                    {
                        Name = !string.IsNullOrEmpty(x.Config.Name)
                            ? x.Config.Name
                            : x.Resource!.Name,
                    },
                ],
            });

        var fromCfGroups = FromCfGroups(qpResources);

        return config.CustomFormats.Concat(fromFormatItems).Concat(fromCfGroups);
    }

    /// Resolves CF group configs to CustomFormatConfig entries by looking up guide resources,
    /// filtering CFs by exclude list, and determining which profiles to assign scores to.
    private IEnumerable<CustomFormatConfig> FromCfGroups(
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

            // Filter out excluded CFs from the group's CF list
            var cfTrashIds = groupResource
                .CustomFormats.Where(cf =>
                    !groupConfig.Exclude.Contains(cf.TrashId, StringComparer.OrdinalIgnoreCase)
                )
                .Select(cf => cf.TrashId)
                .ToList();

            if (cfTrashIds.Count == 0)
            {
                log.Debug("CF group {TrashId} has no CFs after exclusions", groupConfig.TrashId);
                continue;
            }

            yield return new CustomFormatConfig
            {
                TrashIds = cfTrashIds,
                AssignScoresTo = assignScoresTo,
            };
        }
    }

    /// Validates the exclude list for a CF group. Returns true if valid, false if errors found.
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

    /// Returns the list of profiles to assign this group's CFs to. If the user specified
    /// explicit assign_scores_to entries, validates and uses those. Otherwise, uses all
    /// guide-backed quality profiles from the config (filtered by guide exclusions).
    /// Returns null if validation errors occurred for explicit profiles.
    private List<AssignScoresToConfig>? DetermineProfiles(
        CustomFormatGroupConfig groupConfig,
        CfGroupResource groupResource,
        Dictionary<string, QualityProfileResource> qpResources
    )
    {
        // Build set of profile trash_ids excluded by the guide for this group
        var excludedProfiles = new HashSet<string>(
            groupResource.QualityProfiles.Exclude.Values,
            StringComparer.OrdinalIgnoreCase
        );

        if (groupConfig.AssignScoresTo.Count > 0)
        {
            // Explicit: user specified profiles - validate each one
            return ValidateExplicitProfiles(groupConfig, excludedProfiles, qpResources);
        }

        // Implicit: all guide-backed profiles in user's config, filtered by guide exclusions
        // Use config name if set, otherwise fall back to guide resource name
        return config
            .QualityProfiles.Where(qp => qp.TrashId is not null)
            .Where(qp => qpResources.ContainsKey(qp.TrashId!))
            .Where(qp => !excludedProfiles.Contains(qp.TrashId!))
            .Select(qp => new AssignScoresToConfig
            {
                TrashId = qp.TrashId,
                Name = !string.IsNullOrEmpty(qp.Name) ? qp.Name : qpResources[qp.TrashId!].Name,
            })
            .ToList();
    }

    /// Validates explicit assign_scores_to profiles. Returns null if errors found.
    private List<AssignScoresToConfig>? ValidateExplicitProfiles(
        CustomFormatGroupConfig groupConfig,
        HashSet<string> excludedProfiles,
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

            // Check if profile is excluded by guide
            if (excludedProfiles.Contains(score.TrashId))
            {
                events.AddError(
                    $"CF group '{groupConfig.TrashId}': Profile '{score.TrashId}' is excluded by this group's guide definition"
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
