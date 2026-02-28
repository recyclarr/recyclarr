using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Tests.Reusable;

internal class TestPlan() : PipelinePlan(Substitute.For<IDiagnosticPublisher>());

internal static class NewPlan
{
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
        return Qp(config, shouldCreate, null, scores);
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
        bool shouldCreate,
        QualityProfileResource? resource,
        params PlannedCfScore[] scores
    )
    {
        return new PlannedQualityProfile
        {
            Name = resource?.Name ?? config.Name,
            Config = config,
            ShouldCreate = shouldCreate,
            Resource = resource,
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
