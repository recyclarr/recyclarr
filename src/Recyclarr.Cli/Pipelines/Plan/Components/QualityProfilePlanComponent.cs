using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class QualityProfilePlanComponent(
    IInstancePublisher events,
    QualityProfileResourceQuery guide,
    IServiceConfiguration config,
    ILogger log
) : IPlanComponent
{
    public void Process(PipelinePlan plan)
    {
        log.Debug(
            "Planning quality profiles for {Service} {Instance}: {Count} profiles",
            config.ServiceType,
            config.InstanceName,
            config.QualityProfiles.Count
        );

        foreach (var qp in config.QualityProfiles)
        {
            log.Debug(
                "  Profile {Name}: TrashId={TrashId}, MinUpgradeFormatScore={Score}",
                qp.Name,
                qp.TrashId ?? "(none)",
                qp.MinUpgradeFormatScore
            );
        }

        // Load guide resources and build lookup by TrashId
        var guideResources = guide
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        // Build profile-CF pairs from plan's CustomFormats (which includes both config CFs and QP formatItems)
        var profileAndCfs = plan.CustomFormats.SelectMany(cf =>
            cf.AssignScoresTo.Select(scoreConfig => (Profile: scoreConfig, Cf: cf))
        );

        // Start with explicitly defined quality profiles
        var allProfiles = config
            .QualityProfiles.Select(x => CreatePlannedProfile(x, guideResources, events))
            .NotNull()
            .ToDictionary(x => x.Name, x => x, StringComparer.InvariantCultureIgnoreCase);

        foreach (var (scoreConfig, cf) in profileAndCfs)
        {
            if (!allProfiles.TryGetValue(scoreConfig.Name, out var profile))
            {
                // Implicitly add profile if only referenced via assign_scores_to
                allProfiles[scoreConfig.Name] = profile = new PlannedQualityProfile
                {
                    Name = scoreConfig.Name,
                    Config = new QualityProfileConfig { Name = scoreConfig.Name },
                    ShouldCreate = false,
                };
            }

            AddCustomFormatScore(profile, scoreConfig, cf, events);
        }

        // Add all profiles to the plan
        foreach (var profile in allProfiles.Values)
        {
            plan.AddQualityProfile(profile);
        }
    }

    private static void AddCustomFormatScore(
        PlannedQualityProfile profile,
        AssignScoresToConfig scoreConfig,
        PlannedCustomFormat cf,
        IInstancePublisher events
    )
    {
        var scoreToUse = DetermineScore(profile.Config, scoreConfig, cf);
        if (scoreToUse is null)
        {
            return;
        }

        // Check for duplicate
        var existingScore = profile.CfScores.FirstOrDefault(x =>
            x.TrashId.EqualsIgnoreCase(cf.Resource.TrashId)
        );

        if (existingScore is not null)
        {
            if (existingScore.Score != scoreToUse)
            {
                events.AddWarning(
                    $"Custom format {cf.Resource.Name} ({cf.Resource.TrashId}) is duplicated in quality profile "
                        + $"{profile.Name} with conflicting scores: {existingScore.Score} vs {scoreToUse}"
                );
            }
            return;
        }

        profile.CfScores.Add(new PlannedCfScore(cf, scoreToUse.Value));
    }

    private static int? DetermineScore(
        QualityProfileConfig profile,
        AssignScoresToConfig scoreConfig,
        PlannedCustomFormat cf
    )
    {
        // Explicit score from config takes priority
        if (scoreConfig.Score is not null)
        {
            return scoreConfig.Score;
        }

        // Try score set lookup
        if (profile.ScoreSet is not null)
        {
            if (cf.Resource.TrashScores.TryGetValue(profile.ScoreSet, out var scoreFromSet))
            {
                return scoreFromSet;
            }
        }

        // Fall back to default score
        return cf.Resource.DefaultScore;
    }

    private static PlannedQualityProfile? CreatePlannedProfile(
        QualityProfileConfig config,
        Dictionary<string, QualityProfileResource> guideResources,
        IInstancePublisher events
    )
    {
        QualityProfileResource? resource = null;

        if (config.TrashId is not null)
        {
            if (!guideResources.TryGetValue(config.TrashId, out resource))
            {
                events.AddWarning($"Invalid quality profile trash_id: {config.TrashId}");
                return null;
            }
        }

        // Use guide name if available and config name is empty, otherwise use config name
        var name =
            string.IsNullOrEmpty(config.Name) && resource is not null ? resource.Name : config.Name;

        // Build effective config: inherit qualities and score_set from resource if not specified
        var effectiveConfig = BuildEffectiveConfig(config, resource);

        return new PlannedQualityProfile
        {
            Name = name,
            Config = effectiveConfig,
            Resource = resource,
            ShouldCreate = true,
        };
    }

    private static QualityProfileConfig BuildEffectiveConfig(
        QualityProfileConfig config,
        QualityProfileResource? resource
    )
    {
        var result = config;

        // Inherit score_set from resource if not specified in config
        if (config.ScoreSet is null && !string.IsNullOrEmpty(resource?.TrashScoreSet))
        {
            result = result with { ScoreSet = resource.TrashScoreSet };
        }

        // If config already has qualities, return now
        if (config.Qualities.Count > 0)
        {
            return result;
        }

        // If no resource or resource has no items, return unchanged
        if (resource?.Items.Count is null or 0)
        {
            return result;
        }

        // Convert resource items to config qualities format
        var convertedQualities = resource
            .Items.Select(item => new QualityProfileQualityConfig
            {
                Name = item.Name,
                Enabled = item.Allowed,
                Qualities = item.Items,
            })
            .ToList();

        return result with
        {
            Qualities = convertedQualities,
        };
    }
}
