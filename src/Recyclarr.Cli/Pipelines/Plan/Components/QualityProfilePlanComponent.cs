using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.Sync.Events;

namespace Recyclarr.Cli.Pipelines.Plan.Components;

internal class QualityProfilePlanComponent(IServiceConfiguration config, ILogger log)
    : IPlanComponent
{
    public void Process(PipelinePlan plan, ISyncEventPublisher events)
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
                "  Profile {Name}: MinUpgradeFormatScore={Score}",
                qp.Name,
                qp.MinUpgradeFormatScore
            );
        }
        // Build profile-CF pairs from config's assign_scores_to.
        // plan.GetCustomFormat() returns the same PlannedCustomFormat instance that CF plan component
        // added, enabling ID hydration: when CF persistence sets Resource.Id, scores see it via the
        // shared object reference.
        var profileAndCfs = config
            .CustomFormats.SelectMany(x => x.AssignScoresTo.Select(y => (Profile: y, x.TrashIds)))
            .SelectMany(x =>
                x.TrashIds.Select(plan.GetCustomFormat).NotNull().Select(y => (x.Profile, Cf: y))
            );

        // Start with explicitly defined quality profiles
        var allProfiles = config
            .QualityProfiles.Select(x => new PlannedQualityProfile
            {
                Name = x.Name,
                Config = x,
                ShouldCreate = true,
            })
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
        ISyncEventPublisher events
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
}
