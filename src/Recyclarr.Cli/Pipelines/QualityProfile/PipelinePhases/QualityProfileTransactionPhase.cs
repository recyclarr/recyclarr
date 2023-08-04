using System.Diagnostics.CodeAnalysis;
using FluentValidation.Results;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Common.FluentValidation;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

public enum QualityProfileUpdateReason
{
    New,
    Changed
}

public record InvalidProfileData(UpdatedQualityProfile Profile, IReadOnlyCollection<ValidationFailure> Errors);

public record QualityProfileTransactionData
{
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
    public ICollection<UpdatedQualityProfile> UpdatedProfiles { get; set; } = new List<UpdatedQualityProfile>();
    public ICollection<string> NonExistentProfiles { get; init; } = new List<string>();
    public ICollection<InvalidProfileData> InvalidProfiles { get; init; } = new List<InvalidProfileData>();
}

public class QualityProfileTransactionPhase
{
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification =
        "This non-static method establishes a pattern that will eventually become an interface")]
    public QualityProfileTransactionData Execute(
        IReadOnlyCollection<ProcessedQualityProfileData> guideData,
        QualityProfileServiceData serviceData)
    {
        var transactions = new QualityProfileTransactionData();

        BuildUpdatedProfiles(transactions, guideData, serviceData);
        UpdateProfileScores(transactions.UpdatedProfiles);

        ValidateProfiles(transactions);

        return transactions;
    }

    private static void ValidateProfiles(QualityProfileTransactionData transactions)
    {
        var validator = new UpdatedQualityProfileValidator();

        transactions.UpdatedProfiles = transactions.UpdatedProfiles
            .IsValid(validator, (errors, profile) =>
                transactions.InvalidProfiles.Add(new InvalidProfileData(profile, errors)))
            .ToList();
    }

    private static void BuildUpdatedProfiles(
        QualityProfileTransactionData transactions,
        IEnumerable<ProcessedQualityProfileData> guideData,
        QualityProfileServiceData serviceData)
    {
        // Match quality profiles in the user's config to profiles in the service.
        // For each match, we return a tuple including the list of custom format scores ("formatItems").
        // Using GroupJoin() because we want a LEFT OUTER JOIN so we can list which quality profiles in config
        // do not match profiles in Radarr.
        var matchedProfiles = guideData
            .GroupJoin(serviceData.Profiles,
                x => x.Profile.Name,
                x => x.Name,
                (x, y) => (x, y.FirstOrDefault()),
                StringComparer.InvariantCultureIgnoreCase);

        foreach (var (config, dto) in matchedProfiles)
        {
            if (dto is null && !config.ShouldCreate)
            {
                transactions.NonExistentProfiles.Add(config.Profile.Name);
                continue;
            }

            var organizer = new QualityItemOrganizer();
            var newDto = dto ?? serviceData.Schema;

            transactions.UpdatedProfiles.Add(new UpdatedQualityProfile
            {
                ProfileConfig = config,
                ProfileDto = newDto,
                UpdateReason = dto is null ? QualityProfileUpdateReason.New : QualityProfileUpdateReason.Changed,
                UpdatedQualities = organizer.OrganizeItems(newDto, config.Profile)
            });
        }
    }

    private static void UpdateProfileScores(IEnumerable<UpdatedQualityProfile> updatedProfiles)
    {
        foreach (var profile in updatedProfiles)
        {
            profile.UpdatedScores = ProcessScoreUpdates(profile.ProfileConfig, profile.ProfileDto);
        }
    }

    private static List<UpdatedFormatScore> ProcessScoreUpdates(
        ProcessedQualityProfileData profileData,
        QualityProfileDto profileDto)
    {
        var scoreMap = profileData.CfScores
            .FullOuterJoin(profileDto.FormatItems, JoinType.Hash,
                x => x.FormatId,
                x => x.Format,
                // Exists in config, but not in service (these are unusual and should be errors)
                // See `FormatScoreUpdateReason` for reason why we need this (it's preview mode)
                l => UpdatedFormatScore.New(l),
                // Exists in service, but not in config
                r => UpdatedFormatScore.Reset(r, profileData),
                // Exists in both service and config
                (l, r) => UpdatedFormatScore.Updated(r, l))
            .ToList();

        return scoreMap;
    }
}
