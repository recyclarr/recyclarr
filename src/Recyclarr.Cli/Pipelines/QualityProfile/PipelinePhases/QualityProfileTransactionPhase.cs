using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Pipelines.QualityProfile.Cache;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

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

        var updatedProfiles = BuildUpdatedProfiles(
            transactions,
            context.Plan.QualityProfiles,
            context.ApiFetchOutput,
            context.Cache
        );
        UpdateProfileScores(updatedProfiles);

        updatedProfiles = ValidateProfiles(updatedProfiles, transactions.InvalidProfiles);

        AssignProfiles(transactions, updatedProfiles);
        context.TransactionOutput = transactions;

        logger.LogTransactionNotices(context);
        return Task.FromResult(PipelineFlow.Continue);
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

    private List<UpdatedQualityProfile> BuildUpdatedProfiles(
        QualityProfileTransactionData transactions,
        IEnumerable<PlannedQualityProfile> plannedProfiles,
        QualityProfileServiceData serviceData,
        QualityProfileCache cache
    )
    {
        var builder = new UpdatedProfileBuilder(log, serviceData, cache, transactions);
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

        var serviceCfNames = profileDto.FormatItems.Select(x => x.Name).ToList();
        return except
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .Where(x => serviceCfNames.TrueForAll(y => !y.EqualsIgnoreCase(x)))
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
        var scoreMap = profileData
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

        return scoreMap;
    }
}
