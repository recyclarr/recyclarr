using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public record UpdatedQualityProfile(QualityProfileDto UpdatedProfile)
{
    public required IReadOnlyCollection<UpdatedFormatScore> UpdatedScores { get; init; }
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
        var scoreMap = profileData.CfScores
            .FullOuterJoin(profileDto.FormatItems, JoinType.Hash,
                x => x.FormatId,
                x => x.Format,
                // Exists in config, but not in service (these are unusual and should be errors)
                // See `FormatScoreUpdateReason` for reason why we need this (it's preview mode)
                l => new UpdatedFormatScore
                {
                    Dto = new ProfileFormatItemDto {Format = l.FormatId, Name = l.CfName},
                    NewScore = l.Score,
                    Reason = FormatScoreUpdateReason.New
                },
                // Exists in service, but not in config
                r => new UpdatedFormatScore
                {
                    Dto = r,
                    NewScore = profileData.Profile.ResetUnmatchedScores ? 0 : r.Score,
                    Reason = FormatScoreUpdateReason.Reset
                },
                // Exists in both service and config
                (l, r) => new UpdatedFormatScore
                {
                    Dto = r,
                    NewScore = l.Score,
                    Reason = FormatScoreUpdateReason.Updated
                })
            .ToList();

        return scoreMap.Any(x => x.Dto.Score != x.NewScore)
            ? new UpdatedQualityProfile(profileDto) {UpdatedScores = scoreMap}
            : null;
    }
}
