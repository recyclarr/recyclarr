using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Tests;

public static class NewQp
{
    public static ProcessedQualityProfileData Processed(
        string profileName,
        params (string TrashId, int FormatId, int Score)[] scores)
    {
        return Processed(profileName, false, scores);
    }

    public static ProcessedQualityProfileData Processed(
        string profileName,
        bool resetUnmatchedScores,
        params (string TrashId, int FormatId, int Score)[] scores)
    {
        return Processed(profileName, resetUnmatchedScores,
            scores.Select(x => ("", x.TrashId, x.FormatId, x.Score)).ToArray());
    }

    public static ProcessedQualityProfileData Processed(
        string profileName,
        bool resetUnmatchedScores,
        params (string CfName, string TrashId, int FormatId, int Score)[] scores)
    {
        var profileConfig = new QualityProfileConfig
        {
            Name = profileName,
            ResetUnmatchedScores = new ResetUnmatchedScoresConfig
            {
                Enabled = resetUnmatchedScores
            }
        };

        return Processed(profileConfig, scores);
    }

    public static ProcessedQualityProfileData Processed(
        QualityProfileConfig profileConfig,
        params (string CfName, string TrashId, int FormatId, int Score)[] scores)
    {
        return new ProcessedQualityProfileData
        {
            Profile = profileConfig,
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
        return new UpdatedFormatScore(
            new ProfileFormatItemDto {Name = name, Score = oldScore},
            newScore,
            reason);
    }

    public static ProfileItemDto GroupDto(
        int itemId,
        string itemName,
        bool enabled,
        params ProfileItemDto[] nestedItems)
    {
        return new ProfileItemDto
        {
            Id = itemId,
            Name = itemName,
            Allowed = enabled,
            Items = nestedItems
        };
    }

    public static ProfileItemDto QualityDto(int itemId, string itemName, bool enabled)
    {
        return new ProfileItemDto
        {
            Allowed = enabled,
            Quality = new ProfileItemQualityDto
            {
                Id = itemId,
                Name = itemName
            }
        };
    }

    [SuppressMessage("ReSharper", "IntroduceOptionalParameters.Global", Justification =
        "This is for unit test purposes and we want to be explicit sometimes")]
    public static QualityProfileQualityConfig QualityConfig(string itemName)
    {
        return QualityConfig(itemName, true);
    }

    public static QualityProfileQualityConfig QualityConfig(string itemName, bool enabled)
    {
        return new QualityProfileQualityConfig
        {
            Enabled = enabled,
            Name = itemName
        };
    }

    public static QualityProfileQualityConfig GroupConfig(string itemName, params string[] nestedItems)
    {
        return GroupConfig(itemName, true, nestedItems);
    }

    public static QualityProfileQualityConfig GroupConfig(string itemName, bool enabled, params string[] nestedItems)
    {
        return QualityConfig(itemName, enabled) with {Qualities = nestedItems};
    }
}
