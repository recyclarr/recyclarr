using System.Globalization;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class UpdatedProfileBuilder(
    ILogger log,
    QualityProfileServiceData serviceData,
    TrashIdMappingStore state,
    QualityProfileTransactionData transactions
)
{
    private readonly Dictionary<int, QualityProfileData> _serviceProfilesById = serviceData
        .Profiles.Where(p => p.Id.HasValue)
        .ToDictionary(p => p.Id!.Value);

    private readonly ILookup<string, QualityProfileData> _serviceProfilesByName =
        serviceData.Profiles.ToLookup(p => p.Name, StringComparer.OrdinalIgnoreCase);

    private readonly QualityProfileData _schema = serviceData.Schema;
    private readonly IReadOnlyList<ProfileLanguage> _languages = serviceData.Languages;
    private readonly List<UpdatedQualityProfile> _existingProfiles = [];

    // Tracks which state mapping service_ids have been claimed during two-pass resolution.
    // Prevents multiple planned profiles from resolving to the same state entry.
    private readonly HashSet<int> _claimedServiceIds = [];

    public List<UpdatedQualityProfile> BuildFrom(IEnumerable<PlannedQualityProfile> plannedProfiles)
    {
        var profileList = plannedProfiles.ToList();
        var guideBacked = profileList.Where(p => p.Resource is not null).ToList();
        var userDefined = profileList.Where(p => p.Resource is null);

        // Two-pass resolution for guide-backed profiles to support multiple profiles
        // sharing the same trash_id (see docs/architecture/quality-profile-state-resolution.md)
        var unmatchedAfterPass1 = ResolveExactMatches(guideBacked);
        ResolveRenames(unmatchedAfterPass1);

        foreach (var planned in userDefined)
        {
            ProcessUserDefinedProfile(planned);
        }

        return _existingProfiles;
    }

    // Pass 1: exact match by (trash_id, name). Profiles that match are claimed
    // and processed immediately. Returns profiles that didn't find an exact match.
    private List<PlannedQualityProfile> ResolveExactMatches(List<PlannedQualityProfile> guideBacked)
    {
        List<PlannedQualityProfile> unmatched = [];

        foreach (var planned in guideBacked)
        {
            var trashId = planned.Resource!.TrashId;
            var cachedId = state.FindId(new MappingKey(trashId, planned.Name));

            log.Debug(
                "Pass 1: guide QP {TrashId} ({Name}), exact cached ID: {CachedId}",
                trashId,
                planned.Name,
                cachedId?.ToString(CultureInfo.InvariantCulture) ?? "none"
            );

            if (cachedId.HasValue)
            {
                _claimedServiceIds.Add(cachedId.Value);
                ProcessCachedProfile(planned, cachedId.Value);
            }
            else
            {
                unmatched.Add(planned);
            }
        }

        return unmatched;
    }

    // Pass 2: for each unmatched profile, check unclaimed state mappings with the same trash_id.
    // A rename is resolved only when exactly 1 unclaimed mapping and 1 unmatched profile exist
    // for that trash_id. All other combinations fall through to ProcessNameCollision.
    private void ResolveRenames(List<PlannedQualityProfile> unmatched)
    {
        // Group unmatched profiles by trash_id to evaluate rename eligibility per group
        var unmatchedByTrashId = unmatched
            .GroupBy(p => p.Resource!.TrashId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var (trashId, profiles) in unmatchedByTrashId)
        {
            var unclaimed = state
                .Mappings.Where(m =>
                    m.TrashId.Equals(trashId, StringComparison.OrdinalIgnoreCase)
                    && !_claimedServiceIds.Contains(m.ServiceId)
                )
                .ToList();

            log.Debug(
                "Pass 2: trash_id {TrashId} has {Unclaimed} unclaimed mapping(s) "
                    + "and {Unmatched} unmatched profile(s)",
                trashId,
                unclaimed.Count,
                profiles.Count
            );

            if (unclaimed.Count == 1 && profiles.Count == 1)
            {
                // Unambiguous rename: one old mapping, one new profile
                var mapping = unclaimed[0];
                _claimedServiceIds.Add(mapping.ServiceId);
                ProcessCachedProfile(profiles[0], mapping.ServiceId);
            }
            else
            {
                foreach (var planned in profiles)
                {
                    ProcessNameCollision(planned);
                }
            }
        }
    }

    private void ProcessCachedProfile(PlannedQualityProfile planned, int cachedId)
    {
        // Check if target name is already taken by a different service profile.
        // This catches renames that would collide with manually created profiles.
        var nameConflicts = _serviceProfilesByName[planned.Name]
            .Where(p => p.Id != cachedId)
            .ToList();

        if (nameConflicts.Count == 1)
        {
            transactions.ConflictingProfiles.Add(
                new ConflictingQualityProfile(planned, nameConflicts[0].Id!.Value)
            );
            return;
        }

        if (nameConflicts.Count > 1)
        {
            transactions.AmbiguousProfiles.Add(
                new AmbiguousQualityProfile(
                    planned,
                    nameConflicts.Select(p => (p.Name, p.Id!.Value)).ToList()
                )
            );
            return;
        }

        if (_serviceProfilesById.TryGetValue(cachedId, out var serviceProfile))
        {
            if (!serviceProfile.Name.EqualsIgnoreCase(planned.Name))
            {
                log.Debug(
                    "QP {TrashId} will be renamed from '{ServiceName}' to '{GuideName}'",
                    planned.Resource!.TrashId,
                    serviceProfile.Name,
                    planned.Name
                );
            }

            var (fixedProfile, missingQualities) = FixupMissingQualities(serviceProfile);
            AddExistingProfile(planned, fixedProfile, missingQualities);
        }
        else
        {
            log.Debug(
                "Cached service ID {CachedId} for QP {TrashId} no longer exists in service",
                cachedId,
                planned.Resource!.TrashId
            );

            ProcessNameCollision(planned);
        }
    }

    private void ProcessNameCollision(PlannedQualityProfile planned)
    {
        var nameMatches = _serviceProfilesByName[planned.Name].ToList();

        switch (nameMatches.Count)
        {
            case 0:
                if (planned.ShouldCreate)
                {
                    AddNewProfile(planned);
                }
                else
                {
                    transactions.NonExistentProfiles.Add(planned.Config.Name);
                }
                break;

            case 1:
                if (planned.Resource is not null)
                {
                    transactions.ConflictingProfiles.Add(
                        new ConflictingQualityProfile(planned, nameMatches[0].Id!.Value)
                    );
                }
                else
                {
                    var serviceProfile = nameMatches[0];
                    var (fixedProfile, missingQualities) = FixupMissingQualities(serviceProfile);
                    AddExistingProfile(planned, fixedProfile, missingQualities);
                }
                break;

            default:
                transactions.AmbiguousProfiles.Add(
                    new AmbiguousQualityProfile(
                        planned,
                        nameMatches.Select(p => (p.Name, p.Id!.Value)).ToList()
                    )
                );
                break;
        }
    }

    private void ProcessUserDefinedProfile(PlannedQualityProfile planned)
    {
        var nameMatches = _serviceProfilesByName[planned.Name].ToList();

        switch (nameMatches.Count)
        {
            case 0:
                if (planned.ShouldCreate)
                {
                    AddNewProfile(planned);
                }
                else
                {
                    transactions.NonExistentProfiles.Add(planned.Config.Name);
                }
                break;

            case 1:
                var serviceProfile = nameMatches[0];
                var (fixedProfile, missingQualities) = FixupMissingQualities(serviceProfile);
                AddExistingProfile(planned, fixedProfile, missingQualities);
                break;

            default:
                transactions.AmbiguousProfiles.Add(
                    new AmbiguousQualityProfile(
                        planned,
                        nameMatches.Select(p => (p.Name, p.Id!.Value)).ToList()
                    )
                );
                break;
        }
    }

    private void AddNewProfile(PlannedQualityProfile planned)
    {
        var organizer = new QualityItemOrganizer();
        transactions.NewProfiles.Add(
            new UpdatedQualityProfile
            {
                ProfileConfig = planned,
                Profile = _schema,
                Languages = _languages,
                UpdatedQualities = organizer.OrganizeItems(_schema.Items, planned.Config),
            }
        );
    }

    private void AddExistingProfile(
        PlannedQualityProfile planned,
        QualityProfileData profile,
        IReadOnlyCollection<string> missingQualities
    )
    {
        var organizer = new QualityItemOrganizer();
        _existingProfiles.Add(
            new UpdatedQualityProfile
            {
                ProfileConfig = planned,
                Profile = profile,
                Languages = _languages,
                UpdatedQualities = organizer.OrganizeItems(profile.Items, planned.Config),
                MissingQualities = missingQualities,
            }
        );
    }

    // Fixes a rare bug in Sonarr & Radarr where core qualities get lost in existing profiles.
    // Returns a new profile with missing qualities appended, plus their names for diagnostics.
    // See: https://github.com/Radarr/Radarr/issues/9738
    private (QualityProfileData Profile, List<string> MissingNames) FixupMissingQualities(
        QualityProfileData profile
    )
    {
        var missingQualities = _schema
            .Items.FlattenQualities()
            .LeftOuterHashJoin(
                profile.Items.FlattenQualities(),
                l => l.Quality!.Id,
                r => r.Quality!.Id
            )
            .Where(x => x.Right is null)
            .Select(x => x.Left)
            .ToList();

        var fixedProfile = profile with { Items = [.. profile.Items, .. missingQualities] };
        var missingNames = missingQualities
            .Select(x => x.Quality!.Name ?? $"(id: {x.Quality.Id})")
            .ToList();

        return (fixedProfile, missingNames);
    }
}
