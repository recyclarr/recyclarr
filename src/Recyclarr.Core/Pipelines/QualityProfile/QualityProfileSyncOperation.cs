using System.Collections.ObjectModel;
using System.Net;
using System.Text.RegularExpressions;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Pipelines.QualityProfile.State;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;
using Refit;

namespace Recyclarr.Pipelines.QualityProfile;

internal class QualityProfileSyncOperation(
    ILogger log,
    IQualityProfileService service,
    IQualityProfileServiceFields serviceFields,
    IQualityProfileStatePersister statePersister,
    QualityProfileStatCalculator statCalculator,
    QualityProfileLogger logger
) : SyncOperation<PlannedQualityProfiles, QualityProfileComputeResult>
{
    protected override PipelineResult CreateEmptyResult(
        SyncResultStatus status,
        PipelineType? blockedBy
    ) => new QualityProfilePipelineResult(0, 0, [], []).WithStatus(status, blockedBy);

    protected override async Task<QualityProfileComputeResult> Compute(
        PlannedQualityProfiles plan,
        IReadOnlyList<SyncOutcome> planOutcomes,
        CancellationToken ct
    )
    {
        // Fetch phase
        var profilesTask = service.GetQualityProfiles(ct);
        var schemaTask = service.GetSchema(ct);
        var serviceFieldsTask = serviceFields.LoadAsync(ct);
        await Task.WhenAll(profilesTask, schemaTask, serviceFieldsTask);

        var apiFetchOutput = new QualityProfileServiceData(await profilesTask, await schemaTask);
        var state = statePersister.Load();

        // Transaction phase
        var transactions = new QualityProfileTransactionData();

        // Build profiles: new profiles go directly to transactions.NewProfiles,
        // existing profiles are returned for change detection
        var existingProfiles = BuildExistingProfiles(
            transactions,
            plan.Items,
            apiFetchOutput,
            state
        );
        ApplyServiceFields(transactions.NewProfiles.Concat(existingProfiles));

        // Process new profiles: update scores, validate (remove invalid from collection)
        UpdateProfileScores(transactions.NewProfiles);
        RemoveInvalidProfiles(transactions.NewProfiles, transactions.InvalidProfiles);

        // Process existing profiles: update scores, validate, then split by changes
        UpdateProfileScores(existingProfiles);
        existingProfiles = FilterValidProfiles(existingProfiles, transactions.InvalidProfiles);
        AssignExistingProfiles(transactions, existingProfiles);

        var outcomes = BuildOutcomes(planOutcomes, transactions, out var incompleteResources);
        var resources = BuildResources(transactions);
        var completedResources =
            transactions.NewProfiles.Count
            + transactions.UpdatedProfiles.Count
            + transactions.UnchangedProfiles.Count;
        var result = new QualityProfilePipelineResult(
            completedResources,
            incompleteResources,
            outcomes,
            resources
        );

        logger.LogTransactionNotices(transactions);

        return new QualityProfileComputeResult(
            transactions,
            apiFetchOutput.Profiles.Where(p => p.Id.HasValue).Select(p => p.Id!.Value),
            state,
            result
        );
    }

    protected override async Task Persist(
        QualityProfileComputeResult computeResult,
        CancellationToken ct
    )
    {
        var transactions = computeResult.Transactions;
        var completedResources = transactions.UnchangedProfiles.Count;
        var incompleteResources = computeResult.Result.IncompleteResources;
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

        logger.LogPersistenceResults(transactions, createdProfiles, updatedProfiles);
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

    // Service-owned values go on the working profile, so every merged profile built from it
    // (change detection, results, persist) carries them.
    private void ApplyServiceFields(IEnumerable<UpdatedQualityProfile> profiles)
    {
        foreach (var profile in profiles)
        {
            profile.Profile = serviceFields.ApplyDesired(profile.Profile, profile.ProfileConfig);
        }
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
        IReadOnlyList<SyncOutcome> planOutcomes,
        QualityProfileTransactionData transactions,
        out int incompleteResources
    )
    {
        var outcomes = new List<QualityProfileOutcome>();
        incompleteResources = 0;

        foreach (var outcome in planOutcomes)
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
                case QualityProfileSizeOutcome x
                    when QualitySizeOutcomeMapper.Map(x.Inner) is { } sizeOutcome:
                    outcomes.Add(
                        new QualityProfileQualitySizeOutcome(CreateIdentity(x.Profile), sizeOutcome)
                    );
                    if (QualitySizeOutcomeMapper.IsIncomplete(sizeOutcome))
                    {
                        incompleteResources++;
                    }

                    break;
                case ProfileQualityDefinitionIgnoredOutcome x:
                    outcomes.Add(
                        new QualityProfileQualityDefinitionIgnoredOutcome(CreateIdentity(x.Profile))
                    );
                    break;
                case QualityDefinitionUnusedOutcome:
                    outcomes.Add(new QualityProfileQualityDefinitionUnusedOutcome());
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
                case QualityProfileValidationConstraint.QualityReferenceMismatch:
                    yield return new QualityProfileQualityReferenceMismatchOutcome(
                        identity,
                        profile.UpdatedQualities.InvalidQualityNames.ToList()
                    );
                    break;
                case QualityProfileValidationConstraint.ResetScoreReferenceMismatch:
                    yield return new QualityProfileResetScoreReferenceMismatchOutcome(
                        identity,
                        profile.InvalidExceptCfNames.ToList(),
                        profile.InvalidExceptCfPatterns.ToList()
                    );
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported Quality Profile constraint: {constraint}"
                    );
            }
        }
    }

    private List<PlannedQualityProfileResource> BuildResources(
        QualityProfileTransactionData transactions
    )
    {
        var resources = transactions.NewProfiles.Select(BuildCreatedResource).ToList();
        resources.AddRange(
            transactions.UpdatedProfiles.Select(x =>
                BuildExistingResource(x.Profile, PlannedAction.Update, x.QualitiesChanged)
            )
        );
        resources.AddRange(
            transactions.UnchangedProfiles.Select(x =>
                BuildExistingResource(x, PlannedAction.Unchanged, qualitiesChanged: false)
            )
        );
        return resources;
    }

    private PlannedQualityProfileResource BuildCreatedResource(UpdatedQualityProfile profile)
    {
        var desired = profile.BuildMergedProfile();
        var shared = new PlannedQualityProfileResource
        {
            Action = PlannedAction.Create,
            Identity = CreateIdentity(profile.ProfileConfig),
            Name = PlannedValue.Created(desired.Name),
            UpgradeAllowed = PlannedValue.Created(desired.UpgradeAllowed),
            UpgradeUntilQuality = PlannedValue.Created(desired.Items.FindCutoff(desired.Cutoff)),
            UpgradeUntilScore = PlannedValue.Created(desired.CutoffFormatScore),
            MinimumFormatScore = PlannedValue.Created(desired.MinFormatScore),
            MinimumUpgradeFormatScore = PlannedValue.Created(desired.MinUpgradeFormatScore),
            Qualities = PlannedValue.Created<IReadOnlyList<QualityProfileQualityLayoutItem>>(
                MapQualityLayout(desired.Items)
            ),
            CustomFormatScores = MapScores(profile, x => PlannedValue.Created(x.NewScore)),
        };
        return serviceFields.Describe(shared, null, desired, profile.ProfileConfig);
    }

    private PlannedQualityProfileResource BuildExistingResource(
        UpdatedQualityProfile profile,
        PlannedAction action,
        bool qualitiesChanged
    )
    {
        var current = profile.OriginalProfile ?? profile.Profile;
        var desired = profile.BuildMergedProfile();
        var shared = new PlannedQualityProfileResource
        {
            Action = action,
            Identity = CreateIdentity(profile.ProfileConfig),
            Name = PlannedValue.Compared(current.Name, desired.Name),
            UpgradeAllowed = PlannedValue.Compared(current.UpgradeAllowed, desired.UpgradeAllowed),
            UpgradeUntilQuality = PlannedValue.Compared(
                current.Items.FindCutoff(current.Cutoff),
                desired.Items.FindCutoff(desired.Cutoff)
            ),
            UpgradeUntilScore = PlannedValue.Compared(
                current.CutoffFormatScore,
                desired.CutoffFormatScore
            ),
            MinimumFormatScore = PlannedValue.Compared(
                current.MinFormatScore,
                desired.MinFormatScore
            ),
            MinimumUpgradeFormatScore = PlannedValue.Compared(
                current.MinUpgradeFormatScore,
                desired.MinUpgradeFormatScore
            ),
            Qualities = PlannedValue.Compared<IReadOnlyList<QualityProfileQualityLayoutItem>>(
                MapQualityLayout(current.Items),
                MapQualityLayout(desired.Items),
                qualitiesChanged
            ),
            CustomFormatScores = MapScores(
                profile,
                x => PlannedValue.Compared(x.FormatItem.Score, x.NewScore)
            ),
        };
        return serviceFields.Describe(shared, current, desired, profile.ProfileConfig);
    }

    private static List<QualityProfileScoreResult> MapScores(
        UpdatedQualityProfile profile,
        Func<UpdatedFormatScore, PlannedValue<int>> toValue
    )
    {
        return profile
            .UpdatedScores.Select(x => new QualityProfileScoreResult(
                x.FormatItem.Name,
                FindPlannedScore(profile, x)?.TrashId,
                toValue(x),
                x.Reason switch
                {
                    FormatScoreUpdateReason.Reset => QualityProfileScoreReason.Reset,
                    FormatScoreUpdateReason.Unmanaged => QualityProfileScoreReason.Unmanaged,
                    _ => QualityProfileScoreReason.Set,
                }
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
                    ? new QualityProfileQuality
                    {
                        Name = quality.Name ?? item.Name ?? "",
                        Allowed = item.Allowed ?? false,
                    }
                    : new QualityProfileQualityGroup
                    {
                        Name = item.Name ?? "",
                        Allowed = item.Allowed ?? false,
                        Qualities = item
                            .Items.Select(x => x.Quality?.Name ?? x.Name ?? "")
                            .ToList(),
                    }
            )
            .ToList();
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
}
