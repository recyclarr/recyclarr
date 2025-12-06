using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Tests.Reusable;

internal static class NewPlan
{
    public static PlannedCustomFormat Cf(string name, string trashId, int serviceId = 0)
    {
        return new PlannedCustomFormat(
            new CustomFormatResource
            {
                Name = name,
                TrashId = trashId,
                Id = serviceId,
            }
        );
    }

    public static PlannedCfScore CfScore(string trashId, int serviceId, int score)
    {
        return new PlannedCfScore(Cf("", trashId, serviceId), score);
    }

    public static PlannedCfScore CfScore(string name, string trashId, int serviceId, int score)
    {
        return new PlannedCfScore(Cf(name, trashId, serviceId), score);
    }

    public static PlannedQualityProfile Qp(string name, params PlannedCfScore[] scores)
    {
        return Qp(new QualityProfileConfig { Name = name }, scores);
    }

    public static PlannedQualityProfile Qp(
        string name,
        bool shouldCreate,
        params PlannedCfScore[] scores
    )
    {
        return Qp(new QualityProfileConfig { Name = name }, shouldCreate, scores);
    }

    public static PlannedQualityProfile Qp(
        QualityProfileConfig config,
        params PlannedCfScore[] scores
    )
    {
        return Qp(config, true, scores);
    }

    public static PlannedQualityProfile Qp(
        QualityProfileConfig config,
        bool shouldCreate,
        params PlannedCfScore[] scores
    )
    {
        return new PlannedQualityProfile
        {
            Name = config.Name,
            Config = config,
            ShouldCreate = shouldCreate,
            CfScores = scores.ToList(),
        };
    }

    public static PlannedQualitySizes Qs(string type, params PlannedQualityItem[] qualities)
    {
        return Qs(type, null, qualities);
    }

    public static PlannedQualitySizes Qs(
        string type,
        decimal? preferredRatio,
        params PlannedQualityItem[] qualities
    )
    {
        return new PlannedQualitySizes
        {
            Type = type,
            PreferredRatio = preferredRatio,
            Qualities = qualities,
        };
    }

    public static PlannedQualityItem QsItem(
        string quality,
        decimal min,
        decimal? max,
        decimal? preferred
    )
    {
        return new PlannedQualityItem(quality, min, max, preferred);
    }
}
