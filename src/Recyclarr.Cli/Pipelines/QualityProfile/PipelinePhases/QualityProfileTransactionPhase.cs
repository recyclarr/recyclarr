using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public record UpdatedQualityProfile(QualityProfileDto UpdatedProfile)
{
    public Collection<UpdatedFormatScore> UpdatedScores { get; } = new();
}

public record QualityProfileTransactionData
{
    public Collection<string> InvalidProfileNames { get; } = new();
    public Collection<UpdatedQualityProfile> UpdatedProfiles { get; } = new();
}

public class QualityProfileTransactionPhase
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification =
        "This non-static method establishes a pattern that will eventually become an interface")]
    public QualityProfileTransactionData Execute(
        IReadOnlyCollection<ProcessedQualityProfileData> guideData,
        IList<QualityProfileDto> serviceData)
    {
        var transactions = new QualityProfileTransactionData();

        UpdateProfileScores(guideData, serviceData, transactions);

        return transactions;
    }

    private static void UpdateProfileScores(
        IReadOnlyCollection<ProcessedQualityProfileData> guideData,
        IList<QualityProfileDto> serviceData,
        QualityProfileTransactionData transactions)
    {
        // Match quality profiles in Radarr to ones the user put in their config.
        // For each match, we return a tuple including the list of custom format scores ("formatItems").
        // Using GroupJoin() because we want a LEFT OUTER JOIN so we can list which quality profiles in config
        // do not match profiles in Radarr.
        var profilesAndScores = guideData.GroupJoin(serviceData,
            x => x.Profile.Name,
            x => x.Name,
            (x, y) => (x, y.FirstOrDefault()),
            StringComparer.InvariantCultureIgnoreCase);

        foreach (var (profileData, profileDto) in profilesAndScores)
        {
            if (profileDto is null)
            {
                transactions.InvalidProfileNames.Add(profileData.Profile.Name);
                continue;
            }

            var updatedProfile = ProcessScoreUpdates(profileData, profileDto);
            if (updatedProfile is null)
            {
                continue;
            }

            transactions.UpdatedProfiles.Add(updatedProfile);
        }
    }

    private static UpdatedQualityProfile? ProcessScoreUpdates(
        ProcessedQualityProfileData profileData,
        QualityProfileDto profileDto)
    {
        var updatedProfile = new UpdatedQualityProfile(profileDto);

        void UpdateScore(ProfileFormatItemDto item, int newScore, FormatScoreUpdateReason reason)
        {
            if (item.Score == newScore)
            {
                return;
            }

            updatedProfile.UpdatedScores.Add(new UpdatedFormatScore(item.Name, item.Score, newScore, reason));
            item.Score = newScore;
        }

        var scoreMap = profileData.CfScores;

        foreach (var formatItem in profileDto.FormatItems)
        {
            if (scoreMap.TryGetValue(formatItem.Format, out var existingScore))
            {
                UpdateScore(formatItem, existingScore, FormatScoreUpdateReason.Updated);
            }
            else if (profileData.Profile is {ResetUnmatchedScores: true})
            {
                UpdateScore(formatItem, 0, FormatScoreUpdateReason.Reset);
            }
        }

        return updatedProfile.UpdatedScores.Any() ? updatedProfile : null;
    }
}
