using System.Globalization;
using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync.Results;

/// <summary>
/// One instance's terminal sync results with the Sonarr and Radarr response shapes folded into a
/// single view. Only media naming differs between the two services, so it is normalized into
/// <see cref="NamingResults"/>; every other pipeline keeps its response DTO.
/// </summary>
internal sealed record InstanceResults(
    string Name,
    SyncCompletionStatus Status,
    InstanceFailureCategory? Failure,
    SyncFaultResponse? Fault,
    IReadOnlyList<OutcomeResponse> PlanningOutcomes,
    CustomFormatPipelineResponse? CustomFormats,
    QualityProfileResults? QualityProfiles,
    QualitySizePipelineResponse? QualitySizes,
    NamingResults? Naming,
    MediaManagementPipelineResponse? MediaManagement
)
{
    public int CustomFormatChanges =>
        CustomFormats is { } x ? x.Items.Count(IsChanged) + x.Deletes.Count : 0;

    public int QualityProfileChanges => QualityProfiles?.Changes.Count ?? 0;

    public int QualitySizeChanges => QualitySizes?.Items.Count(IsChanged) ?? 0;

    public int NamingChanges => Naming?.Changes.Count ?? 0;

    public int MediaManagementChanges =>
        MediaManagement?.Settings is { Action: not ResourceAction.Unchanged } ? 1 : 0;

    public static bool IsChanged(CustomFormatItemResponse item) =>
        item.Action != ResourceAction.Unchanged;

    public static bool IsChanged(QualitySizeItemResponse item) =>
        item.Action != ResourceAction.Unchanged;

    public static InstanceResults From(SyncInstanceResultsResponse instance) =>
        instance switch
        {
            SyncInstanceResultsResponseSonarr x => new InstanceResults(
                x.Name,
                x.Status,
                x.Failure,
                x.Fault,
                [.. x.PlanningOutcomes ?? []],
                x.Pipelines.CustomFormats,
                x.Pipelines.QualityProfiles is { } qp ? QualityProfileResults.From(qp) : null,
                x.Pipelines.QualitySizes,
                x.Pipelines.Naming is { } naming ? NamingResults.From(naming) : null,
                x.Pipelines.MediaManagement
            ),
            SyncInstanceResultsResponseRadarr x => new InstanceResults(
                x.Name,
                x.Status,
                x.Failure,
                x.Fault,
                [.. x.PlanningOutcomes ?? []],
                x.Pipelines.CustomFormats,
                x.Pipelines.QualityProfiles is { } qp ? QualityProfileResults.From(qp) : null,
                x.Pipelines.QualitySizes,
                x.Pipelines.Naming is { } naming ? NamingResults.From(naming) : null,
                x.Pipelines.MediaManagement
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(instance), instance, null),
        };
}

/// <summary>
/// Media naming results for either service. <see cref="Changes"/> holds one row per naming field
/// the sync changes, labeled for display.
/// </summary>
internal sealed record NamingResults(
    string Title,
    PipelineStatus Status,
    BlockingPipeline? BlockedBy,
    IReadOnlyList<OutcomeResponse> Outcomes,
    IReadOnlyList<FieldChange> Changes
)
{
    public static NamingResults From(SonarrNamingPipelineResponse pipeline) =>
        new(
            "Sonarr Media Naming",
            pipeline.Status,
            pipeline.BlockedBy,
            [.. pipeline.Outcomes],
            pipeline.Settings is not { } x
                ? []
                :
                [
                    .. new[]
                    {
                        FieldChange.From("Enable Episode Renames?", x.RenameEpisodes),
                        FieldChange.From("Series Folder", x.SeriesFolderFormat),
                        FieldChange.From("Season Folder", x.SeasonFolderFormat),
                        FieldChange.From("Standard Episodes", x.StandardEpisodeFormat),
                        FieldChange.From("Daily Episodes", x.DailyEpisodeFormat),
                        FieldChange.From("Anime Episodes", x.AnimeEpisodeFormat),
                    }.OfType<FieldChange>(),
                ]
        );

    public static NamingResults From(RadarrNamingPipelineResponse pipeline) =>
        new(
            "Radarr Media Naming",
            pipeline.Status,
            pipeline.BlockedBy,
            [.. pipeline.Outcomes],
            pipeline.Settings is not { } x
                ? []
                :
                [
                    .. new[]
                    {
                        FieldChange.From("Enable Movie Renames?", x.RenameMovies),
                        FieldChange.From("Movie", x.StandardMovieFormat),
                        FieldChange.From("Folder", x.MovieFolderFormat),
                    }.OfType<FieldChange>(),
                ]
        );
}

/// <summary>
/// Quality profile results for either service. Each created or updated profile is normalized into
/// a <see cref="ProfileChange"/>, so fields only one service has (Radarr's language) appear as
/// extra field rows.
/// </summary>
internal sealed record QualityProfileResults(
    PipelineStatus Status,
    BlockingPipeline? BlockedBy,
    IReadOnlyList<OutcomeResponse> Outcomes,
    IReadOnlyList<ProfileChange> Changes
)
{
    public static QualityProfileResults From(SonarrQualityProfilePipelineResponse pipeline) =>
        new(
            pipeline.Status,
            pipeline.BlockedBy,
            [.. pipeline.Outcomes],
            [
                .. pipeline
                    .Items.Where(x => x.Action != ResourceAction.Unchanged)
                    .Select(x =>
                        Change(
                            x.Action,
                            x.Identity,
                            SharedFields(
                                x.Name,
                                x.UpgradeAllowed,
                                x.MinimumFormatScore,
                                x.MinimumUpgradeFormatScore,
                                x.UpgradeUntilQuality,
                                x.UpgradeUntilScore
                            ),
                            x.Qualities,
                            x.CustomFormatScores
                        ) with
                        {
                            Sizes =
                            [
                                .. x
                                    .QualitySizes.Where(y => y.Action != ResourceAction.Unchanged)
                                    .Select(SizeChange.From),
                            ],
                        }
                    ),
            ]
        );

    public static QualityProfileResults From(RadarrQualityProfilePipelineResponse pipeline) =>
        new(
            pipeline.Status,
            pipeline.BlockedBy,
            [.. pipeline.Outcomes],
            [
                .. pipeline
                    .Items.Where(x => x.Action != ResourceAction.Unchanged)
                    .Select(x =>
                        Change(
                            x.Action,
                            x.Identity,
                            [
                                .. SharedFields(
                                    x.Name,
                                    x.UpgradeAllowed,
                                    x.MinimumFormatScore,
                                    x.MinimumUpgradeFormatScore,
                                    x.UpgradeUntilQuality,
                                    x.UpgradeUntilScore
                                ),
                                .. new[]
                                {
                                    FieldChange.From("Language", x.Language.Value),
                                }.OfType<FieldChange>(),
                            ],
                            x.Qualities,
                            x.CustomFormatScores
                        )
                    ),
            ]
        );

    private static IEnumerable<FieldChange> SharedFields(
        PlannedValueResponseOfString name,
        QualityProfileFieldResponseOfNullableOfBoolean upgradeAllowed,
        QualityProfileFieldResponseOfNullableOfInt32 minimumFormatScore,
        QualityProfileFieldResponseOfNullableOfInt32 minimumUpgradeFormatScore,
        QualityProfileFieldResponseOfString upgradeUntilQuality,
        QualityProfileFieldResponseOfNullableOfInt32 upgradeUntilScore
    ) =>
        new[]
        {
            FieldChange.From("Name", name),
            FieldChange.From("Upgrades Allowed?", upgradeAllowed.Value),
            FieldChange.From("Minimum Format Score", minimumFormatScore.Value),
            FieldChange.From("Minimum Format Upgrade Score", minimumUpgradeFormatScore.Value),
            FieldChange.From("Upgrade Until Quality", upgradeUntilQuality.Value),
            FieldChange.From("Upgrade Until Score", upgradeUntilScore.Value),
        }.OfType<FieldChange>();

    // A created profile lists every score it sets; an updated one lists only changed scores.
    private static ProfileChange Change(
        ResourceAction action,
        QualityProfileIdentityResponse identity,
        IEnumerable<FieldChange> fields,
        QualityProfileFieldResponseOfIReadOnlyListOfQualityProfileLayoutResponse qualityField,
        ICollection<QualityProfileScoreResponse> scores
    )
    {
        var created = action == ResourceAction.Create;
        var shownScores = created
            ? scores.Where(x => x.Reason != QualityProfileScoreReason.Unmanaged)
            : scores.Where(x => x.Value.Changed);
        var qualities = qualityField.Value;

        return new ProfileChange(
            identity,
            created ? "New" : "Changed",
            [.. fields],
            qualities.Changed ? [.. qualities.Current ?? []] : [],
            qualities.Changed ? [.. qualities.Desired ?? []] : [],
            [
                .. shownScores.Select(x => new ScoreChange(
                    x.Name,
                    FieldChange.Text(x.Value.Current),
                    FieldChange.Text(x.Value.Desired),
                    created ? "Set" : x.Reason.ToString()
                )),
            ],
            []
        );
    }
}

/// <summary>
/// One profile the sync creates or updates, ready for display. Empty
/// <see cref="DesiredQualities"/> means the quality layout does not change.
/// </summary>
internal sealed record ProfileChange(
    QualityProfileIdentityResponse Identity,
    string Reason,
    IReadOnlyList<FieldChange> Fields,
    IReadOnlyList<QualityProfileLayoutResponse> CurrentQualities,
    IReadOnlyList<QualityProfileLayoutResponse> DesiredQualities,
    IReadOnlyList<ScoreChange> Scores,
    IReadOnlyList<SizeChange> Sizes
);

internal sealed record ScoreChange(string Name, string Current, string Desired, string Reason);

/// <summary>
/// Per-quality size changes on a profile (Sonarr v5), each value formatted for display.
/// </summary>
internal sealed record SizeChange(string Quality, string Minimum, string Maximum, string Preferred)
{
    private const string Unchanged = "[dim]unchanged[/]";

    // A created profile shows each size it sets; an updated one shows changes only.
    public static SizeChange From(QualitySizeItemResponse size) =>
        size.Action == ResourceAction.Create
            ? new SizeChange(
                size.Quality,
                QualitySizeText.Of(size.Minimum.Desired),
                QualitySizeText.Of(size.Maximum.Desired),
                QualitySizeText.Of(size.Preferred.Desired)
            )
            : new SizeChange(
                size.Quality,
                Text(size.Minimum),
                Text(size.Maximum),
                Text(size.Preferred)
            );

    public static string Text(PlannedValueResponseOfQualitySizeValueResponse value) =>
        value.Changed
            ? $"{QualitySizeText.Of(value.Current)} -> {QualitySizeText.Of(value.Desired)}"
            : Unchanged;
}

/// <summary>
/// A labeled current-to-desired change of one settings field, with both values already formatted
/// for display.
/// </summary>
internal sealed record FieldChange(string Field, string Current, string Desired)
{
    public const string Unset = "UNSET";

    // Each returns null for an absent (unconfigured) or unchanged value.
    public static FieldChange? From(string field, PlannedValueResponseOfString? value) =>
        value is { Changed: true }
            ? new FieldChange(field, Text(value.Current), Text(value.Desired))
            : null;

    public static FieldChange? From(string field, PlannedValueResponseOfNullableOfBoolean? value) =>
        value is { Changed: true }
            ? new FieldChange(field, Text(value.Current), Text(value.Desired))
            : null;

    public static FieldChange? From(string field, PlannedValueResponseOfNullableOfInt32? value) =>
        value is { Changed: true }
            ? new FieldChange(field, Text(value.Current), Text(value.Desired))
            : null;

    public static FieldChange? From(
        string field,
        PlannedValueResponseOfNullableOfPropersAndRepacksModeResponse? value
    ) =>
        value is { Changed: true }
            ? new FieldChange(field, Text(value.Current), Text(value.Desired))
            : null;

    public static string Text(string? value) => value ?? Unset;

    public static string Text(bool? value) =>
        value switch
        {
            true => "Yes",
            false => "No",
            null => Unset,
        };

    public static string Text(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? Unset;

    private static string Text(PropersAndRepacksModeResponse? value) => value?.ToString() ?? Unset;
}
