using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.QualityProfile.PipelinePhases;

namespace Recyclarr.TrashLib.TestLibrary;

public static class NewQp
{
    public static ProcessedQualityProfileData Processed(
        string profileName,
        params (int FormatId, int Score)[] scores)
    {
        return Processed(profileName, false, scores);
    }

    public static ProcessedQualityProfileData Processed(
        string profileName,
        bool resetUnmatchedScores,
        params (int FormatId, int Score)[] scores)
    {
        return new ProcessedQualityProfileData(new QualityProfileConfig(profileName, resetUnmatchedScores))
        {
            CfScores = scores.ToDictionary(x => x.FormatId, x => x.Score)
        };
    }
}
