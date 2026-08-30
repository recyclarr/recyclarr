using System.Collections.ObjectModel;
using System.Net;
using System.Text.RegularExpressions;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Pipelines.QualityProfile.State;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;
using Refit;

namespace Recyclarr.Pipelines.QualityProfile;

internal class QualityProfileSyncOperation(
    ILogger log,
    IQualityProfileService service,
    IQualityProfileStatePersister statePersister,
    QualityProfileStatCalculator statCalculator,
    QualityProfileLogger logger
) : SyncOperation<QualityProfileComputeResult>
{
    public override PipelineType Type => PipelineType.QualityProfile;
    public override string Description => "Quality Profile";
    public override IReadOnlyList<PipelineType> Dependencies => [PipelineType.CustomFormat];

    protected override async Task<QualityProfileComputeResult> Compute(
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        // Fetch phase
        var profilesTask = service.GetQualityProfiles(ct);
        var schemaTask = service.GetSchema(ct);
        var languagesTask = service.GetLanguages(ct);
        await Task.WhenAll(profilesTask, schemaTask, languagesTask);

        var apiFetchOutput = new QualityProfileServiceData(
            await profilesTask,
            await schemaTask,
            await languagesTask
        );
        var state = statePersister.Load();

        // Transaction phase
        var transactions = new QualityProfileTransactionData();

        // Build profiles: new profiles go directly to transactions.NewProfiles,
        // existing profiles are returned for change detection
        var existingProfiles = BuildExistingProfiles(
            transactions,
            plan.QualityProfiles,
            apiFetchOutput,
            state
        );

        // Process new profiles: update scores, validate (remove invalid from collection)
        UpdateProfileScores(transactions.NewProfiles);
        RemoveInvalidProfiles(transactions.NewProfiles, transactions.InvalidProfiles);

        // Process existing profiles: update scores, validate, then split by changes
        UpdateProfileScores(existingProfiles);
        existingProfiles = FilterValidProfiles(existingProfiles, transactions.InvalidProfiles);
        AssignExistingProfiles(transactions, existingProfiles);

        var outcomes = BuildOutcomes(plan, transactions, out var incompleteResources);
        var deltas = BuildDeltas(transactions);
        var completedResources =
            transactions.NewProfiles.Count
            + transactions.UpdatedProfiles.Count
            + transactions.UnchangedProfiles.Count;
        var result = new QualityProfilePipelineResult(
            completedResources,
            incompleteResources,
            outcomes,
            deltas
        );

        logger.LogTransactionNotices(transactions, publisher);
        QualityProfileLogger.SetStatus(publisher, result);

        return new QualityProfileComputeResult(
            transactions,
            apiFetchOutput.Profiles.Where(p => p.Id.HasValue).Select(p => p.Id!.Value),
            state,
            result,
            incompleteResources
        );
    }

    protected override async Task Persist(
        QualityProfileComputeResult computeResult,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var transactions = computeResult.Transactions;
        var completedResources = transactions.UnchangedProfiles.Count;
        var incompleteResources = computeResult.TransactionIncompleteResources;
        var persistenceOutcomes = new List<QualityProfileOutcome>();
        var createdProfiles = new List<UpdatedQualityProfile>();
        var updatedProfiles = new List<ProfileWithStats>();

        // Create new profiles
        foreach (var profile in transactions.NewProfiles)
        {
            try
            {
                profile.Profile = await service.CreateQualityProfile(
                    profile.BuildMergedProfile(),
                    ct
                );
                computeResult.RecordSynced(profile);
                createdProfiles.Add(profile);
                completedResources++;
            }
            catch (ApiException e) when (IsResourceRejection(e))
            {
                incompleteResources++;
                persistenceOutcomes.Add(
                    new QualityProfileCreateRejectedOutcome(CreateIdentity(profile.ProfileConfig))
                );
            }
        }

        // Update existing profiles with changes
        foreach (var profileWithStats in transactions.UpdatedProfiles)
        {
            try
            {
                var profile = profileWithStats.Profile;
                await service.UpdateQualityProfile(profile.BuildMergedProfile(), ct);
                computeResult.RecordSynced(profile);
                updatedProfiles.Add(profileWithStats);
                completedResources++;
            }
            catch (ApiException e) when (IsResourceRejection(e))
            {
                incompleteResources++;
                persistenceOutcomes.Add(
                    new QualityProfileUpdateRejectedOutcome(
                        CreateIdentity(profileWithStats.Profile.ProfileConfig)
                    )
                );
            }
        }

        computeResult.State.Update(computeResult);
        statePersister.Save(computeResult.State);
        computeResult.CompleteApply(completedResources, incompleteResources, persistenceOutcomes);

        logger.LogPersistenceResults(
            transactions,
            publisher,
            computeResult.Result,
            createdProfiles,
            updatedProfiles
        );
    }

    private static bool IsResourceRejection(ApiException exception)
    {
        return exception.StatusCode
            is HttpStatusCode.BadRequest
                or HttpStatusCode.NotFound
                or HttpStatusCode.Conflict
                or HttpStatusCode.UnprocessableEntity;
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
        TrashIdMappingStore state
    )
    {
        var builder = new UpdatedProfileBuilder(log, serviceData, state, transactions);
        return builder.BuildFrom(plannedProfiles);
    }

    private static void UpdateProfileScores(IEnumerable<UpdatedQualityProfile> updatedProfiles)
    {
        foreach (var profile in updatedProfiles)
        {
            var resetConfig = profile.ProfileConfig.Config.ResetUnmatchedScores;

            profile.InvalidExceptCfNames = GetInvalidExceptCfNames(resetConfig, profile.Profile);
            profile.InvalidExceptCfPatterns = GetInvalidExceptCfPatterns(
                resetConfig,
                profile.Profile
            );

            profile.UpdatedScores = ProcessScoreUpdates(profile.ProfileConfig, profile.Profile);
        }
    }

    private static List<string> GetInvalidExceptCfNames(
        ResetUnmatchedScoresConfig resetConfig,
        QualityProfileData profile
    )
    {
        var except = resetConfig.Except;
        if (except.Count == 0)
        {
            return [];
        }

        return except
            .Except(
                profile.FormatItems.Select(x => x.Name),
                StringComparer.InvariantCultureIgnoreCase
            )
            .ToList();
    }

    // Find patterns that don't match any CF in the profile
    private static List<string> GetInvalidExceptCfPatterns(
        ResetUnmatchedScoresConfig resetConfig,
        QualityProfileData profile
    )
    {
        var patterns = resetConfig.ExceptPatterns;
        if (patterns.Count == 0)
        {
            return [];
        }

        var cfNames = profile.FormatItems.Select(x => x.Name).ToList();
        return patterns
            .Where(pattern =>
                !cfNames.Any(name => Regex.IsMatch(name, pattern, RegexOptions.IgnoreCase))
            )
            .ToList();
    }

    private static List<UpdatedFormatScore> ProcessScoreUpdates(
        PlannedQualityProfile profileData,
        QualityProfileData profile
    )
    {
        return profileData
            .CfScores.FullOuterHashJoin(
                profile.FormatItems,
                x => x.ServiceId,
                x => x.FormatId,
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

    private static List<QualityProfileOutcome> BuildOutcomes(
        PipelinePlan plan,
        QualityProfileTransactionData transactions,
        out int incompleteResources
    )
    {
        var outcomes = new List<QualityProfileOutcome>();
        incompleteResources = 0;

        foreach (var outcome in plan.Outcomes)
        {
            switch (outcome)
            {
                case InvalidQualityProfileTrashIdOutcome x:
                    outcomes.Add(new QualityProfileReferenceMismatchOutcome(x.TrashId));
                    incompleteResources++;
                    break;
                case DuplicateQualityProfileNameOutcome x:
                    outcomes.Add(new QualityProfileDuplicateNameOutcome(x.Name));
                    incompleteResources++;
                    break;
                case CustomFormatServiceIdCollisionOutcome x:
                    outcomes.Add(
                        new QualityProfileScoreCollisionOutcome(
                            new QualityProfileCustomFormatReference(
                                x.ExistingName,
                                x.ExistingTrashId
                            ),
                            new QualityProfileCustomFormatReference(x.NewName, x.NewTrashId),
                            x.ServiceId
                        )
                    );
                    incompleteResources++;
                    break;
            }
        }

        foreach (var profile in transactions.NonExistentProfiles)
        {
            outcomes.Add(new QualityProfileNotFoundOutcome(CreateIdentity(profile)));
            incompleteResources++;
        }

        outcomes.AddRange(
            transactions.ReplacedProfiles.Select(x => new QualityProfileAdoptedOutcome(
                CreateIdentity(x.Profile),
                x.ServiceId
            ))
        );

        foreach (var invalid in transactions.InvalidProfiles)
        {
            outcomes.AddRange(MapValidationOutcomes(invalid));
            incompleteResources++;
        }

        foreach (var conflict in transactions.RenameConflicts)
        {
            outcomes.Add(
                new QualityProfileRenameBlockedOutcome(
                    CreateIdentity(conflict.Profile),
                    new QualityProfileServiceMatch(conflict.ConflictName, conflict.ConflictId)
                )
            );
            incompleteResources++;
        }

        foreach (var ambiguous in transactions.AmbiguousProfiles)
        {
            outcomes.Add(
                new QualityProfileAmbiguousMatchOutcome(
                    CreateIdentity(ambiguous.PlannedProfile),
                    ambiguous
                        .ServiceMatches.Select(x => new QualityProfileServiceMatch(x.Name, x.Id))
                        .ToList()
                )
            );
            incompleteResources++;
        }

        foreach (var profile in CompletedProfiles(transactions))
        {
            var identity = CreateIdentity(profile.ProfileConfig);
            if (profile.UpdatedQualities.InvalidQualityNames.Count > 0)
            {
                outcomes.Add(
                    new QualityProfileQualityReferenceMismatchOutcome(
                        identity,
                        profile.UpdatedQualities.InvalidQualityNames.ToList()
                    )
                );
            }

            if (profile.InvalidExceptCfNames.Count > 0 || profile.InvalidExceptCfPatterns.Count > 0)
            {
                outcomes.Add(
                    new QualityProfileResetScoreReferenceMismatchOutcome(
                        identity,
                        profile.InvalidExceptCfNames.ToList(),
                        profile.InvalidExceptCfPatterns.ToList()
                    )
                );
            }
        }

        return outcomes;
    }

    private static IEnumerable<QualityProfileOutcome> MapValidationOutcomes(
        InvalidProfileData invalid
    )
    {
        var profile = invalid.Profile;
        var identity = CreateIdentity(profile.ProfileConfig);

        foreach (var error in invalid.Errors)
        {
            if (
                !Enum.TryParse<QualityProfileValidationConstraint>(
                    error.ErrorCode,
                    out var constraint
                )
            )
            {
                throw new InvalidOperationException(
                    $"Unsupported Quality Profile validation code: {error.ErrorCode}"
                );
            }

            switch (constraint)
            {
                case QualityProfileValidationConstraint.MinimumScoreUnsatisfied:
                {
                    var scores = profile.UpdatedScores.Select(x => x.NewScore).ToList();
                    yield return new QualityProfileMinimumScoreUnsatisfiedOutcome(
                        identity,
                        profile.EffectiveMinFormatScore ?? 0,
                        scores.Where(x => x > 0).Sum(),
                        scores.Count > 0 ? scores.Max() : 0
                    );
                    break;
                }
                case QualityProfileValidationConstraint.InvalidCutoff:
                    yield return new QualityProfileInvalidCutoffOutcome(
                        identity,
                        profile.ProfileConfig.Config.UpgradeUntilQuality ?? ""
                    );
                    break;
                case QualityProfileValidationConstraint.UnavailableCutoff:
                    yield return new QualityProfileUnavailableCutoffOutcome(
                        identity,
                        profile.ProfileConfig.Config.UpgradeUntilQuality ?? ""
                    );
                    break;
                case QualityProfileValidationConstraint.QualitiesRequired:
                    yield return new QualityProfileQualitiesRequiredOutcome(identity);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported Quality Profile constraint: {constraint}"
                    );
            }
        }
    }

    private static List<QualityProfileDelta> BuildDeltas(QualityProfileTransactionData transactions)
    {
        var deltas = transactions
            .NewProfiles.Select<UpdatedQualityProfile, QualityProfileDelta>(
                profile => new QualityProfileCreateDelta(
                    CreateIdentity(profile.ProfileConfig),
                    BuildControlledState(profile)
                )
            )
            .ToList();

        deltas.AddRange(
            transactions.UpdatedProfiles.Select(x => new QualityProfileUpdateDelta(
                CreateIdentity(x.Profile.ProfileConfig),
                BuildUpdateComponents(x)
            ))
        );
        return deltas;
    }

    private static QualityProfileControlledState BuildControlledState(UpdatedQualityProfile profile)
    {
        var planned = profile.ProfileConfig;
        var config = planned.Config;
        var guide = planned.GuideResource;

        return new QualityProfileControlledState(
            profile.EffectiveName,
            config.UpgradeAllowed ?? guide?.UpgradeAllowed,
            config.UpgradeUntilQuality ?? NullIfEmpty(guide?.Cutoff),
            config.UpgradeUntilScore ?? guide?.CutoffFormatScore,
            config.MinFormatScore ?? guide?.MinFormatScore,
            config.MinUpgradeFormatScore ?? guide?.MinUpgradeFormatScore,
            NullIfEmpty(guide?.Language),
            config.Qualities.Count > 0 ? MapQualityLayout(profile.BuildMergedProfile().Items) : [],
            MapControlledScores(profile)
        );
    }

    private static List<QualityProfileUpdateComponent> BuildUpdateComponents(
        ProfileWithStats profileWithStats
    )
    {
        var profile = profileWithStats.Profile;
        var current = profile.OriginalProfile ?? profile.Profile;
        var desired = profile.BuildMergedProfile();
        var components = new List<QualityProfileUpdateComponent>();

        AddChanged(current.Name, desired.Name, value => new QualityProfileNameChanged(value));
        AddChanged(
            current.UpgradeAllowed,
            desired.UpgradeAllowed,
            value => new QualityProfileUpgradeAllowedChanged(value)
        );
        AddChanged(
            current.Items.FindCutoff(current.Cutoff),
            desired.Items.FindCutoff(desired.Cutoff),
            value => new QualityProfileUpgradeUntilQualityChanged(value)
        );
        AddChanged(
            current.CutoffFormatScore,
            desired.CutoffFormatScore,
            value => new QualityProfileUpgradeUntilScoreChanged(value)
        );
        AddChanged(
            current.MinFormatScore,
            desired.MinFormatScore,
            value => new QualityProfileMinimumFormatScoreChanged(value)
        );
        AddChanged(
            current.MinUpgradeFormatScore,
            desired.MinUpgradeFormatScore,
            value => new QualityProfileMinimumUpgradeFormatScoreChanged(value)
        );
        AddChanged(
            current.Language?.Name,
            desired.Language?.Name,
            value => new QualityProfileLanguageChanged(value)
        );

        if (profileWithStats.QualitiesChanged)
        {
            components.Add(
                new QualityProfileQualityLayoutChanged(
                    MapQualityLayout(current.Items),
                    MapQualityLayout(desired.Items)
                )
            );
        }

        components.AddRange(
            profile
                .UpdatedScores.Where(x => x.FormatItem.Score != x.NewScore)
                .Select(x =>
                {
                    var plannedScore = FindPlannedScore(profile, x);
                    return new QualityProfileCustomFormatScoreChanged(
                        x.FormatItem.Name,
                        plannedScore?.TrashId,
                        new ValueDelta<int>(x.FormatItem.Score, x.NewScore),
                        x.Reason == FormatScoreUpdateReason.Reset
                            ? QualityProfileScoreChangeReason.Reset
                            : QualityProfileScoreChangeReason.Set
                    );
                })
        );

        return components;

        void AddChanged<T>(
            T currentValue,
            T desiredValue,
            Func<ValueDelta<T>, QualityProfileUpdateComponent> create
        )
        {
            if (!EqualityComparer<T>.Default.Equals(currentValue, desiredValue))
            {
                components.Add(create(new ValueDelta<T>(currentValue, desiredValue)));
            }
        }
    }

    private static List<QualityProfileCustomFormatScore> MapControlledScores(
        UpdatedQualityProfile profile
    )
    {
        return profile
            .UpdatedScores.Select(x => (Score: x, Planned: FindPlannedScore(profile, x)))
            .Where(x => x.Score.Reason != FormatScoreUpdateReason.NoChange || x.Planned is not null)
            .Select(x => new QualityProfileCustomFormatScore(
                x.Score.FormatItem.Name,
                x.Planned?.TrashId,
                x.Score.NewScore
            ))
            .ToList();
    }

    private static PlannedCfScore? FindPlannedScore(
        UpdatedQualityProfile profile,
        UpdatedFormatScore score
    )
    {
        return profile.ProfileConfig.CfScores.FirstOrDefault(x =>
                x.ServiceId > 0 && x.ServiceId == score.FormatItem.FormatId
            )
            ?? profile.ProfileConfig.CfScores.FirstOrDefault(x =>
                x.Name.Equals(score.FormatItem.Name, StringComparison.OrdinalIgnoreCase)
            );
    }

    private static List<QualityProfileQualityLayoutItem> MapQualityLayout(
        IReadOnlyList<QualityProfileItem> items
    )
    {
        return items
            .Select<QualityProfileItem, QualityProfileQualityLayoutItem>(item =>
                item.Quality is { } quality
                    ? new QualityProfileQuality(
                        quality.Name ?? item.Name ?? "",
                        item.Allowed ?? false
                    )
                    : new QualityProfileQualityGroup(
                        item.Name ?? "",
                        item.Allowed ?? false,
                        item.Items.Select(x => x.Quality?.Name ?? x.Name ?? "").ToList()
                    )
            )
            .ToList();
    }

    private static IEnumerable<UpdatedQualityProfile> CompletedProfiles(
        QualityProfileTransactionData transactions
    )
    {
        return transactions
            .NewProfiles.Concat(transactions.UpdatedProfiles.Select(x => x.Profile))
            .Concat(transactions.UnchangedProfiles);
    }

    private static QualityProfileIdentity CreateIdentity(PlannedQualityProfile profile)
    {
        return profile switch
        {
            PlannedQualityProfile.GuideBacked guide => new GuideBackedQualityProfileIdentity(
                new MappingKey(guide.Resource.TrashId, guide.Name)
            ),
            PlannedQualityProfile.UserDefined => new UserDefinedQualityProfileIdentity(
                profile.Name
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        };
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
