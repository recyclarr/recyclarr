using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualityProfile.Models;
using Recyclarr.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Pipelines.QualityProfile.State;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Sync;
using Recyclarr.SyncState;

namespace Recyclarr.Pipelines.QualityProfile;

internal class QualityProfileSyncOperation(
    ILogger log,
    IQualityProfileService service,
    IQualityProfileStatePersister statePersister,
    QualityProfileStatCalculator statCalculator,
    QualityProfileLogger logger,
    IEnumerable<IPreviewRenderer<QualityProfileTransactionData>> previewRenderers
) : ISyncOperation, ISyncStateSource
{
    private QualityProfileServiceData _apiFetchOutput = null!;
    private QualityProfileTransactionData _transactionOutput = null!;
    private TrashIdMappingStore _state = null!;

    public PipelineType Type => PipelineType.QualityProfile;
    public string Description => "Quality Profile";
    public IReadOnlyList<PipelineType> Dependencies => [PipelineType.CustomFormat];

    public bool ShouldSkip(PipelinePlan plan) => false;

    // ISyncStateSource implementation
    // Only store guide-backed profiles (those with a valid service ID).
    public IEnumerable<TrashIdMapping> SyncedMappings =>
        _transactionOutput
            .NewProfiles.Concat(_transactionOutput.UnchangedProfiles)
            .Concat(_transactionOutput.UpdatedProfiles.Select(x => x.Profile))
            .Select(ToMapping)
            .OfType<TrashIdMapping>();

    private static TrashIdMapping? ToMapping(UpdatedQualityProfile p) =>
        p
            is {
                Profile.Id: { } serviceId,
                ProfileConfig: PlannedQualityProfile.GuideBacked guideBacked,
            }
            ? new TrashIdMapping(guideBacked.Resource.TrashId, p.ProfileName, serviceId)
            : null;

    // QP has no delete flag - entries removed only when service ID no longer exists
    public IEnumerable<int> DeletedIds => [];

    public IEnumerable<int> ValidServiceIds =>
        _apiFetchOutput.Profiles.Where(p => p.Id.HasValue).Select(p => p.Id!.Value);

    public async Task Compute(PipelinePlan plan, IPipelinePublisher publisher, CancellationToken ct)
    {
        // Fetch phase
        var profilesTask = service.GetQualityProfiles(ct);
        var schemaTask = service.GetSchema(ct);
        var languagesTask = service.GetLanguages(ct);
        await Task.WhenAll(profilesTask, schemaTask, languagesTask);

        _apiFetchOutput = new QualityProfileServiceData(
            await profilesTask,
            await schemaTask,
            await languagesTask
        );
        _state = statePersister.Load();

        // Transaction phase
        var transactions = new QualityProfileTransactionData();

        // Build profiles: new profiles go directly to transactions.NewProfiles,
        // existing profiles are returned for change detection
        var existingProfiles = BuildExistingProfiles(
            transactions,
            plan.QualityProfiles,
            _apiFetchOutput,
            _state
        );

        // Process new profiles: update scores, validate (remove invalid from collection)
        UpdateProfileScores(transactions.NewProfiles);
        RemoveInvalidProfiles(transactions.NewProfiles, transactions.InvalidProfiles);

        // Process existing profiles: update scores, validate, then split by changes
        UpdateProfileScores(existingProfiles);
        existingProfiles = FilterValidProfiles(existingProfiles, transactions.InvalidProfiles);
        AssignExistingProfiles(transactions, existingProfiles);

        _transactionOutput = transactions;

        logger.LogTransactionNotices(_transactionOutput, publisher);
    }

    public async Task Persist(IPipelinePublisher publisher, CancellationToken ct)
    {
        var transactions = _transactionOutput;

        // Create new profiles
        foreach (var profile in transactions.NewProfiles)
        {
            profile.Profile = await service.CreateQualityProfile(profile.BuildMergedProfile(), ct);
        }

        // Update existing profiles with changes
        foreach (var profileWithStats in transactions.UpdatedProfiles)
        {
            var merged = profileWithStats.Profile.BuildMergedProfile();
            await service.UpdateQualityProfile(merged, ct);
        }

        _state.Update(this);
        statePersister.Save(_state);

        logger.LogPersistenceResults(_transactionOutput, publisher);
    }

    public void RenderPreview(string instanceName)
    {
        var renderer = previewRenderers.FirstOrDefault();
        renderer?.Render(Description, instanceName, _transactionOutput);
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

    [SuppressMessage(
        "ReSharper",
        "ConvertClosureToMethodGroup",
        Justification = "Keep New() for readability and consistency"
    )]
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
}
