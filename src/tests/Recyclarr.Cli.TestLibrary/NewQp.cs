using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.TestLibrary;

public static class NewQp
{
    public static ProcessedQualityProfileData Processed(
        string profileName,
        params (string TrashId, int FormatId, int Score)[] scores)
    {
        return Processed(profileName, null, scores);
    }

    public static ProcessedQualityProfileData Processed(
        string profileName,
        bool? resetUnmatchedScores,
        params (string TrashId, int FormatId, int Score)[] scores)
    {
        return Processed(profileName, resetUnmatchedScores,
            scores.Select(x => ("", x.TrashId, x.FormatId, x.Score)).ToArray());
    }

    public static ProcessedQualityProfileData Processed(
        string profileName,
        bool? resetUnmatchedScores,
        params (string CfName, string TrashId, int FormatId, int Score)[] scores)
    {
        return new ProcessedQualityProfileData(new QualityProfileConfig
        {
            Name = profileName, ResetUnmatchedScores = resetUnmatchedScores
        })
        {
            CfScores = scores
                .Select(x => new ProcessedQualityProfileScore(x.TrashId, x.CfName, x.FormatId, x.Score))
                .ToList()
        };
    }

    public static UpdatedFormatScore UpdatedScore(
        string name,
        int oldScore,
        int newScore,
        FormatScoreUpdateReason reason)
    {
        return new UpdatedFormatScore
        {
            Dto = new ProfileFormatItemDto {Name = name, Score = oldScore},
            NewScore = newScore,
            Reason = reason
        };
    }
}
