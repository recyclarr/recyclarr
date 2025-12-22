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
                log.Debug("CF group {TrashId} not found in guide resources", groupConfig.TrashId);
                continue;
            }

            // Determine which profiles this group's CFs should be assigned to
            var assignScoresTo = DetermineProfiles(groupConfig, groupResource, qpResources);
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

    /// Returns the list of profiles to assign this group's CFs to. If the user specified
    /// explicit assign_scores_to entries, uses those (filtered by guide exclusions). Otherwise,
    /// uses all guide-backed quality profiles from the config (also filtered by guide exclusions).
    private List<AssignScoresToConfig> DetermineProfiles(
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
            // Explicit: user specified profiles, filtered by guide exclusions
            // Resolve trash_id to profile name via guide resources
            return groupConfig
                .AssignScoresTo.Where(score => !excludedProfiles.Contains(score.TrashId))
                .Where(score => qpResources.ContainsKey(score.TrashId))
                .Select(score => new AssignScoresToConfig
                {
                    TrashId = score.TrashId,
                    Name = qpResources[score.TrashId].Name,
                })
                .ToList();
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
}
