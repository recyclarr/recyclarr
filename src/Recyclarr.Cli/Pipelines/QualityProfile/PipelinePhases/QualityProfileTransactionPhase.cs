using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Cli.Pipelines.QualityProfile.State;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfileTransactionPhase(
    ILogger log,
    QualityProfileStatCalculator statCalculator,
    QualityProfileLogger logger
) : IPipelinePhase<QualityProfilePipelineContext>
{
    public Task<PipelineFlow> Execute(QualityProfilePipelineContext context, CancellationToken ct)
    {
        var transactions = new QualityProfileTransactionData();

        // Build profiles: new profiles go directly to transactions.NewProfiles,
        // existing profiles are returned for change detection
        var existingProfiles = BuildExistingProfiles(
            transactions,
            context.Plan.QualityProfiles,
            context.ApiFetchOutput,
            context.State
        );

        // Process new profiles: update scores, validate (remove invalid from collection)
        UpdateProfileScores(transactions.NewProfiles);
        RemoveInvalidProfiles(transactions.NewProfiles, transactions.InvalidProfiles);

        // Process existing profiles: update scores, validate, then split by changes
        UpdateProfileScores(existingProfiles);
        existingProfiles = FilterValidProfiles(existingProfiles, transactions.InvalidProfiles);
        AssignExistingProfiles(transactions, existingProfiles);

        context.TransactionOutput = transactions;

        logger.LogTransactionNotices(context);
        return Task.FromResult(PipelineFlow.Continue);
    }

    private void AssignExistingProfiles(
        QualityProfileTransactionData transactions,
        IEnumerable<UpdatedQualityProfile> existingProfiles
    )
    {
        foreach (var profile in existingProfiles)
        {
            var stats = statCalculator.Calculate(profile);
            var hasChanges = stats.ProfileChanged || stats.ScoresChanged || stats.QualitiesChanged;

            if (hasChanges)
            {
                transactions.UpdatedProfiles.Add(stats);
            }
            else
            {
                transactions.UnchangedProfiles.Add(profile);
            }
        }
    }

    private static List<UpdatedQualityProfile> FilterValidProfiles(
        IEnumerable<UpdatedQualityProfile> profiles,
        Collection<InvalidProfileData> invalidProfiles
    )
    {
        var validator = new UpdatedQualityProfileValidator();

        return profiles
            .IsValid(
                validator,
                (errors, profile) => invalidProfiles.Add(new InvalidProfileData(profile, errors))
            )
            .ToList();
    }

    private static void RemoveInvalidProfiles(
        Collection<UpdatedQualityProfile> profiles,
        Collection<InvalidProfileData> invalidProfiles
    )
    {
        var validator = new UpdatedQualityProfileValidator();
        var validProfiles = profiles
            .IsValid(
                validator,
                (errors, profile) => invalidProfiles.Add(new InvalidProfileData(profile, errors))
            )
            .ToList();

        profiles.Clear();
        foreach (var profile in validProfiles)
        {
            profiles.Add(profile);
        }
    }

    private List<UpdatedQualityProfile> BuildExistingProfiles(
        QualityProfileTransactionData transactions,
        IEnumerable<PlannedQualityProfile> plannedProfiles,
        QualityProfileServiceData serviceData,
        TrashIdMappingStore<QualityProfileMappings> state
    )
    {
        var builder = new UpdatedProfileBuilder(log, serviceData, state, transactions);
        return builder.BuildFrom(plannedProfiles);
    }

    private static void UpdateProfileScores(IEnumerable<UpdatedQualityProfile> updatedProfiles)
    {
        foreach (var profile in updatedProfiles)
        {
            profile.InvalidExceptCfNames = GetInvalidExceptCfNames(
                profile.ProfileConfig.Config.ResetUnmatchedScores,
                profile.ProfileDto
            );

            profile.UpdatedScores = ProcessScoreUpdates(profile.ProfileConfig, profile.ProfileDto);
        }
    }

    private static List<string> GetInvalidExceptCfNames(
        ResetUnmatchedScoresConfig resetConfig,
        QualityProfileDto profileDto
    )
    {
        var except = resetConfig.Except;
        if (except.Count == 0)
        {
            return [];
        }

        return except
            .Except(
                profileDto.FormatItems.Select(x => x.Name),
                StringComparer.InvariantCultureIgnoreCase
            )
            .ToList();
    }

    [SuppressMessage(
        "ReSharper",
        "ConvertClosureToMethodGroup",
        Justification = "Keep New() for readability and consistency"
    )]
    private static List<UpdatedFormatScore> ProcessScoreUpdates(
        PlannedQualityProfile profileData,
        QualityProfileDto profileDto
    )
    {
        return profileData
            .CfScores.FullOuterHashJoin(
                profileDto.FormatItems,
                x => x.ServiceId,
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
    }
}
