using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfileTransactionPhase(
    QualityProfileStatCalculator statCalculator,
    QualityProfileLogger logger
) : IPipelinePhase<QualityProfilePipelineContext>
{
    public Task<bool> Execute(QualityProfilePipelineContext context, CancellationToken ct)
    {
        var transactions = new QualityProfileTransactionData();

        var updatedProfiles = BuildUpdatedProfiles(
            transactions,
            context.ConfigOutput,
            context.ApiFetchOutput
        );
        UpdateProfileScores(updatedProfiles);

        updatedProfiles = ValidateProfiles(updatedProfiles, transactions.InvalidProfiles);

        AssignProfiles(transactions, updatedProfiles);
        context.TransactionOutput = transactions;

        logger.LogTransactionNotices(context);
        return Task.FromResult(true);
    }

    private void AssignProfiles(
        QualityProfileTransactionData transactions,
        IEnumerable<UpdatedQualityProfile> updatedProfiles
    )
    {
        var profilesWithStats = updatedProfiles
            .Select(statCalculator.Calculate)
            .ToLookup(x => x.HasChanges);

        transactions.UnchangedProfiles = profilesWithStats[false].ToList();
        transactions.ChangedProfiles = profilesWithStats[true].ToList();
    }

    private static List<UpdatedQualityProfile> ValidateProfiles(
        IEnumerable<UpdatedQualityProfile> transactions,
        ICollection<InvalidProfileData> invalidProfiles
    )
    {
        var validator = new UpdatedQualityProfileValidator();

        return transactions
            .IsValid(
                validator,
                (errors, profile) => invalidProfiles.Add(new InvalidProfileData(profile, errors))
            )
            .ToList();
    }

    private static List<UpdatedQualityProfile> BuildUpdatedProfiles(
        QualityProfileTransactionData transactions,
        IEnumerable<ProcessedQualityProfileData> processedConfig,
        QualityProfileServiceData serviceData
    )
    {
        // Match quality profiles in the user's config to profiles in the service.
        // For each match, we return a tuple including the list of custom format scores ("formatItems").
        // Using GroupJoin() because we want a LEFT OUTER JOIN so we can list which quality profiles in config
        // do not match profiles in Radarr.
        var matchedProfiles = processedConfig.GroupJoin(
            serviceData.Profiles,
            x => x.Profile.Name,
            x => x.Name,
            (x, y) => (x, y.FirstOrDefault()),
            StringComparer.InvariantCultureIgnoreCase
        );

        var updatedProfiles = new List<UpdatedQualityProfile>();

        foreach (var (config, dto) in matchedProfiles)
        {
            if (dto is null && !config.ShouldCreate)
            {
                transactions.NonExistentProfiles.Add(config.Profile.Name);
                continue;
            }

            var organizer = new QualityItemOrganizer();

            if (dto is null)
            {
                AddDto(serviceData.Schema, QualityProfileUpdateReason.New);
            }
            else
            {
                var missingQualities = FixupMissingQualities(dto, serviceData.Schema);
                AddDto(dto, QualityProfileUpdateReason.Changed);
                updatedProfiles[^1].MissingQualities = missingQualities;
            }

            continue;

            void AddDto(QualityProfileDto newDto, QualityProfileUpdateReason reason)
            {
                updatedProfiles.Add(
                    new UpdatedQualityProfile
                    {
                        ProfileConfig = config,
                        ProfileDto = newDto,
                        UpdateReason = reason,
                        UpdatedQualities = organizer.OrganizeItems(newDto, config.Profile),
                    }
                );
            }
        }

        return updatedProfiles;
    }

    private static List<string> FixupMissingQualities(
        QualityProfileDto dto,
        QualityProfileDto schema
    )
    {
        // There's a very rare bug in Sonarr & Radarr that results in core qualities being lost in an existing profile.
        // It's unclear how this happens; but what ends up happening is that you get an error "Must contain all
        // qualities" in the Sonarr frontend when you open a QP and simply click save. In Recyclarr, you also see this
        // error when attempting to sync changes to that profile. While this bug is not caused by recyclarr, we do not
        // want this to prevent users from having to sync. The workaround to fix this (linked below) is very cumbersome,
        // so there's value in having Recyclarr transparently fix this for users.
        //
        // See: https://github.com/Radarr/Radarr/issues/9738
        var missingQualities = schema
            .Items.FlattenQualities()
            .LeftOuterHashJoin(dto.Items.FlattenQualities(), l => l.Quality!.Id, r => r.Quality!.Id)
            .Where(x => x.Right is null)
            .Select(x => x.Left)
            .ToList();

        dto.Items = dto.Items.Concat(missingQualities).ToList();
        return missingQualities.Select(x => x.Quality!.Name ?? $"(id: {x.Quality.Id})").ToList();
    }

    private static void UpdateProfileScores(IEnumerable<UpdatedQualityProfile> updatedProfiles)
    {
        foreach (var profile in updatedProfiles)
        {
            profile.InvalidExceptCfNames = GetInvalidExceptCfNames(
                profile.ProfileConfig.Profile.ResetUnmatchedScores,
                profile.ProfileDto
            );

            profile.UpdatedScores = ProcessScoreUpdates(profile.ProfileConfig, profile.ProfileDto);
        }
    }

    private static IReadOnlyCollection<string> GetInvalidExceptCfNames(
        ResetUnmatchedScoresConfig resetConfig,
        QualityProfileDto profileDto
    )
    {
        var except = resetConfig.Except;
        if (except.Count == 0)
        {
            return Array.Empty<string>();
        }

        var serviceCfNames = profileDto.FormatItems.Select(x => x.Name).ToList();
        return except
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .Where(x => serviceCfNames.TrueForAll(y => !y.EqualsIgnoreCase(x)))
            .ToList();
    }

    private static List<UpdatedFormatScore> ProcessScoreUpdates(
        ProcessedQualityProfileData profileData,
        QualityProfileDto profileDto
    )
    {
        var scoreMap = profileData
            .CfScores.FullOuterHashJoin(
                profileDto.FormatItems,
                x => x.FormatId,
                x => x.Format,
                // Exists in config, but not in service (these are unusual and should be errors)
                // See `FormatScoreUpdateReason` for reason why we need this (it's preview mode)
                l => UpdatedFormatScore.New(l),
                // Exists in service, but not in config
                r => UpdatedFormatScore.Reset(r, profileData),
                // Exists in both service and config
                (l, r) => UpdatedFormatScore.Updated(r, l)
            )
            .ToList();

        return scoreMap;
    }
}
