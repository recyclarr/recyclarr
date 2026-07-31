using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Sync;
using Riok.Mapperly.Abstractions;

namespace Recyclarr.Server.Features.Sync.GetJobResults;

// Projects a sync run's domain transaction data onto the wire DTOs at the HTTP boundary, and
// deliberately narrows the domain down to what consumers render: full custom format bodies and
// quality profile internals never reach the wire.
//
// Straight member copies are generated; the projections that reshape (merging collections behind a
// discriminator, filtering, deriving a desired state from a method call) are written by hand and
// picked up by the generator as user mappings.
//
// Target-only strict mapping: every response member must have a source, so adding a member to a DTO
// without wiring it up is a build error. Requiring the reverse would be noise, since these DTOs
// intentionally drop most of the domain.
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class SyncResultsResponseMapper
{
    public static SyncInstanceResultResponse ToResponse(
        this SyncInstanceResult result,
        string instanceName
    )
    {
        return new SyncInstanceResultResponse(instanceName)
        {
            CustomFormats = MapCustomFormats(result.CustomFormats),
            QualityProfiles = MapQualityProfiles(result.QualityProfiles),
            QualitySizes = MapQualitySizes(result.QualitySizes),
            SonarrNaming = MapSonarrNaming(result.SonarrNaming),
            RadarrNaming = MapRadarrNaming(result.RadarrNaming),
            MediaManagement = MapMediaManagement(result.MediaManagement),
        };
    }

    private static CustomFormatsResultResponse? MapCustomFormats(CustomFormatSyncResult? result)
    {
        if (result is null)
        {
            return null;
        }

        var t = result.Transactions;

        // Three collections keyed by different element types collapse into one action-tagged list;
        // consumers render them as rows of a single table.
        var changes = t
            .NewCustomFormats.Select(cf => MapChange("Create", cf.Name, cf.TrashId, result))
            .Concat(
                t.UpdatedCustomFormats.Select(cf =>
                    MapChange("Update", cf.Name, cf.TrashId, result)
                )
            )
            .Concat(
                t.DeletedCustomFormats.Select(m => MapChange("Delete", m.Name, m.TrashId, result))
            )
            .ToList();

        return new CustomFormatsResultResponse
        {
            Changes = changes,
            UnchangedCount = t.UnchangedCustomFormats.Count,
        };
    }

    private static CustomFormatChangeResponse MapChange(
        string action,
        string name,
        string trashId,
        CustomFormatSyncResult result
    )
    {
        var info = result.SourceInfo.GetValueOrDefault(trashId);
        return new CustomFormatChangeResponse(action, name, trashId)
        {
            Source = MapSource(info),
            InclusionReason = info is null or { InclusionReason: CfInclusionReason.None }
                ? null
                : info.InclusionReason.ToString(),
        };
    }

    private static partial CustomFormatSourceResponse? MapSource(CustomFormatSourceInfo? info);

    private static QualityProfilesResultResponse? MapQualityProfiles(
        QualityProfileSyncResult? result
    )
    {
        if (result is null)
        {
            return null;
        }

        var t = result.Transactions;

        // Collection membership is the change reason (ADR-009); flatten both into one tagged list.
        var profiles = t
            .NewProfiles.Select(p => MapProfile(p, "New"))
            .Concat(t.UpdatedProfiles.Select(p => MapProfile(p.Profile, "Changed")))
            .ToList();

        return new QualityProfilesResultResponse { Profiles = profiles };
    }

    private static QualityProfileChangeResponse MapProfile(
        UpdatedQualityProfile profile,
        string changeReason
    )
    {
        return new QualityProfileChangeResponse(profile.ProfileName, changeReason)
        {
            Current = MapFields(profile.Profile),
            // The desired state is computed, not stored, so it cannot come from a member mapping.
            Desired = MapFields(profile.BuildMergedProfile()),
            Qualities = profile.HasQualityOverrides
                ? new QualityProfileQualitiesResponse(
                    profile.QualitySort.ToString(),
                    profile.Profile.Items.Select(MapQualityItem).ToList(),
                    profile.UpdatedQualities.Items.Select(MapQualityItem).ToList()
                )
                : null,
            ScoreChanges = MapScoreChanges(profile.UpdatedScores),
        };
    }

    [MapProperty(
        nameof(QualityProfileData.CutoffFormatScore),
        nameof(QualityProfileFieldsResponse.UpgradeUntilScore)
    )]
    [MapPropertyFromSource(
        nameof(QualityProfileFieldsResponse.UpgradeUntilQuality),
        Use = nameof(MapCutoffQualityName)
    )]
    private static partial QualityProfileFieldsResponse MapFields(QualityProfileData profile);

    // Cutoff is an id on the profile; consumers want the quality it names.
    private static string? MapCutoffQualityName(QualityProfileData profile)
    {
        return profile.Items.FindCutoff(profile.Cutoff);
    }

    private static QualityItemResponse MapQualityItem(QualityProfileItem item)
    {
        // A group carries its name directly; a plain quality carries it on the nested quality.
        return new QualityItemResponse(item.Quality?.Name ?? item.Name ?? "", item.Allowed is true)
        {
            Items = item.Items.Select(MapQualityItem).ToList(),
        };
    }

    // A reason of NoChange, or an unchanged value, is noise for a consumer rendering a list of
    // score updates.
    private static List<FormatScoreChangeResponse> MapScoreChanges(
        IReadOnlyCollection<UpdatedFormatScore> scores
    )
    {
        return scores
            .Where(x =>
                x.Reason != FormatScoreUpdateReason.NoChange && x.FormatItem.Score != x.NewScore
            )
            .Select(MapScoreChange)
            .ToList();
    }

    [MapProperty("FormatItem.Name", nameof(FormatScoreChangeResponse.Name))]
    [MapProperty("FormatItem.Score", nameof(FormatScoreChangeResponse.CurrentScore))]
    private static partial FormatScoreChangeResponse MapScoreChange(UpdatedFormatScore score);

    // Hand-written for the filter: only sizes that differ from the service are worth sending.
    private static QualitySizesResultResponse? MapQualitySizes(QualitySizeSyncResult? result)
    {
        if (result is null)
        {
            return null;
        }

        return new QualitySizesResultResponse
        {
            Items = result.Items.Where(x => x.IsDifferent).Select(MapQualitySizeItem).ToList(),
            MaxLimit = result.Limits.MaxLimit,
            PreferredLimit = result.Limits.PreferredLimit,
        };
    }

    private static partial QualitySizeItemResponse MapQualitySizeItem(UpdatedQualityItem item);

    // Only the desired state is projected for the three operations below: their renderers show what
    // the instance will end up with, not a before/after comparison.

    [MapNestedProperties(nameof(SonarrNamingSyncResult.Desired))]
    private static partial SonarrNamingResultResponse? MapSonarrNaming(
        SonarrNamingSyncResult? result
    );

    [MapNestedProperties(nameof(RadarrNamingSyncResult.Desired))]
    private static partial RadarrNamingResultResponse? MapRadarrNaming(
        RadarrNamingSyncResult? result
    );

    [MapNestedProperties(nameof(MediaManagementSyncResult.Desired))]
    private static partial MediaManagementResultResponse? MapMediaManagement(
        MediaManagementSyncResult? result
    );
}
