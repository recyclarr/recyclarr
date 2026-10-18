using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Core.Tests.Reusable;

internal static class NewPlan
{
    public static PlannedQualityProfile Qp(string name, params PlannedCfScore[] scores)
    {
        return Qp(new QualityProfileConfig { Name = name }, scores);
    }

    public static PlannedQualityProfile Qp(
        string name,
        bool isManaged,
        params PlannedCfScore[] scores
    )
    {
        return Qp(new QualityProfileConfig { Name = name }, isManaged, scores);
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
        bool isManaged,
        params PlannedCfScore[] scores
    )
    {
        return Qp(config, isManaged, null, scores);
    }

    public static PlannedQualityProfile Qp(
        QualityProfileConfig config,
        QualityProfileResource resource,
        params PlannedCfScore[] scores
    )
    {
        return Qp(config, true, resource, scores);
    }

    public static PlannedQualityProfile Qp(
        QualityProfileConfig config,
        bool isManaged,
        QualityProfileResource? resource,
        params PlannedCfScore[] scores
    )
    {
        return resource is not null
            ? new PlannedQualityProfile.GuideBacked
            {
                Name = resource.Name,
                Config = config,
                IsManaged = isManaged,
                Resource = resource,
                CfScores = scores.ToList(),
            }
            : new PlannedQualityProfile.UserDefined
            {
                Name = config.Name,
                Config = config,
                IsManaged = isManaged,
                CfScores = scores.ToList(),
            };
    }

    public static QualityProfileResource QpResource(string trashId, string name)
    {
        return new QualityProfileResource { TrashId = trashId, Name = name };
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
