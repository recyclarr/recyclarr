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
    IReadOnlyList<PlanningOutcomeResponse> PlanningOutcomes,
    CustomFormatPipelineResponse? CustomFormats,
    QualityProfileResults? QualityProfiles,
    QualitySizePipelineResponse? QualitySizes,
    NamingResults? Naming,
    MediaManagementPipelineResponse? MediaManagement
)
{
    public int CustomFormatChanges =>
        CustomFormats is { } x ? x.Creates.Count + x.Updates.Count + x.Deletes.Count : 0;

    public int QualityProfileChanges => QualityProfiles?.Changes.Count ?? 0;

    public int QualitySizeChanges => QualitySizes?.Updates.Count ?? 0;

    public int NamingChanges => Naming?.Changes.Count ?? 0;

    public int MediaManagementChanges => MediaManagement?.Updates.Count ?? 0;

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
    IReadOnlyList<NamingMismatch> Mismatches,
    IReadOnlyList<FieldChange> Changes
)
{
    public static NamingResults From(SonarrNamingPipelineResponse pipeline) =>
        new(
            "Sonarr Media Naming",
            pipeline.Status,
            pipeline.BlockedBy,
            [
                .. pipeline.Outcomes.ReferenceMismatches.Select(x => new NamingMismatch(
                    x.Field.ToString(),
                    x.ConfiguredKey
                )),
            ],
            [
                .. pipeline.Updates.SelectMany(x =>
                    new[]
                    {
                        FieldChange.From("Enable Episode Renames?", x.RenameEpisodes),
                        FieldChange.From("Series Folder", x.SeriesFolderFormat),
                        FieldChange.From("Season Folder", x.SeasonFolderFormat),
                        FieldChange.From("Standard Episodes", x.StandardEpisodeFormat),
                        FieldChange.From("Daily Episodes", x.DailyEpisodeFormat),
                        FieldChange.From("Anime Episodes", x.AnimeEpisodeFormat),
                    }.OfType<FieldChange>()
                ),
            ]
        );

    public static NamingResults From(RadarrNamingPipelineResponse pipeline) =>
        new(
            "Radarr Media Naming",
            pipeline.Status,
            pipeline.BlockedBy,
            [
                .. pipeline.Outcomes.ReferenceMismatches.Select(x => new NamingMismatch(
                    x.Field.ToString(),
                    x.ConfiguredKey
                )),
            ],
            [
                .. pipeline.Updates.SelectMany(x =>
                    new[]
                    {
                        FieldChange.From("Enable Movie Renames?", x.RenameMovies),
                        FieldChange.From("Movie", x.StandardMovieFormat),
                        FieldChange.From("Folder", x.MovieFolderFormat),
                    }.OfType<FieldChange>()
                ),
            ]
        );
}

internal sealed record NamingMismatch(string Field, string ConfiguredKey);

/// <summary>
/// Quality profile results for either service. Each created or updated profile is normalized into
/// a <see cref="ProfileChange"/>, so fields only one service has (Radarr's language) appear as
/// extra field rows.
/// </summary>
internal sealed record QualityProfileResults(
    PipelineStatus Status,
    BlockingPipeline? BlockedBy,
    QualityProfileOutcomesResponse Outcomes,
    IReadOnlyList<ProfileChange> Changes
)
{
    private const string Unset = FieldChange.Unset;

    public static QualityProfileResults From(SonarrQualityProfilePipelineResponse pipeline) =>
        new(
            pipeline.Status,
            pipeline.BlockedBy,
            pipeline.Outcomes,
            [
                .. pipeline.Creates.Select(x =>
                    Create(
                        x.Identity,
                        CreatedFields(
                            x.State.Name,
                            x.State.UpgradeAllowed,
                            x.State.MinimumFormatScore,
                            x.State.MinimumUpgradeFormatScore,
                            x.State.UpgradeUntilQuality,
                            x.State.UpgradeUntilScore
                        ),
                        x.State.Qualities,
                        x.State.CustomFormatScores
                    )
                ),
                .. pipeline.Updates.Select(x =>
                    Update(
                        x.Identity,
                        UpdatedFields(
                            x.Name,
                            x.UpgradeAllowed,
                            x.MinimumFormatScore,
                            x.MinimumUpgradeFormatScore,
                            x.UpgradeUntilQuality,
                            x.UpgradeUntilScore
                        ),
                        x.QualityLayout,
                        x.CustomFormatScores
                    )
                ),
            ]
        );

    public static QualityProfileResults From(RadarrQualityProfilePipelineResponse pipeline) =>
        new(
            pipeline.Status,
            pipeline.BlockedBy,
            pipeline.Outcomes,
            [
                .. pipeline.Creates.Select(x =>
                    Create(
                        x.Identity,
                        [
                            .. CreatedFields(
                                x.State.Name,
                                x.State.UpgradeAllowed,
                                x.State.MinimumFormatScore,
                                x.State.MinimumUpgradeFormatScore,
                                x.State.UpgradeUntilQuality,
                                x.State.UpgradeUntilScore
                            ),
                            new FieldChange("Language", Unset, FieldChange.Text(x.State.Language)),
                        ],
                        x.State.Qualities,
                        x.State.CustomFormatScores
                    )
                ),
                .. pipeline.Updates.Select(x =>
                    Update(
                        x.Identity,
                        [
                            .. UpdatedFields(
                                x.Name,
                                x.UpgradeAllowed,
                                x.MinimumFormatScore,
                                x.MinimumUpgradeFormatScore,
                                x.UpgradeUntilQuality,
                                x.UpgradeUntilScore
                            ),
                            .. new[]
                            {
                                FieldChange.From("Language", x.Language),
                            }.OfType<FieldChange>(),
                        ],
                        x.QualityLayout,
                        x.CustomFormatScores
                    )
                ),
            ]
        );

    private static List<FieldChange> CreatedFields(
        string name,
        bool? upgradeAllowed,
        int? minimumFormatScore,
        int? minimumUpgradeFormatScore,
        string? upgradeUntilQuality,
        int? upgradeUntilScore
    ) =>
        [
            new("Name", Unset, name),
            new("Upgrades Allowed?", Unset, FieldChange.Text(upgradeAllowed)),
            new("Minimum Format Score", Unset, FieldChange.Text(minimumFormatScore)),
            new("Minimum Format Upgrade Score", Unset, FieldChange.Text(minimumUpgradeFormatScore)),
            new("Upgrade Until Quality", Unset, FieldChange.Text(upgradeUntilQuality)),
            new("Upgrade Until Score", Unset, FieldChange.Text(upgradeUntilScore)),
        ];

    private static List<FieldChange> UpdatedFields(
        ValueChangeResponseOfString? name,
        ValueChangeResponseOfNullableOfBoolean? upgradeAllowed,
        ValueChangeResponseOfNullableOfInt32? minimumFormatScore,
        ValueChangeResponseOfNullableOfInt32? minimumUpgradeFormatScore,
        ValueChangeResponseOfString? upgradeUntilQuality,
        ValueChangeResponseOfNullableOfInt32? upgradeUntilScore
    ) =>
        [
            .. new[]
            {
                FieldChange.From("Name", name),
                FieldChange.From("Upgrades Allowed?", upgradeAllowed),
                FieldChange.From("Minimum Format Score", minimumFormatScore),
                FieldChange.From("Minimum Format Upgrade Score", minimumUpgradeFormatScore),
                FieldChange.From("Upgrade Until Quality", upgradeUntilQuality),
                FieldChange.From("Upgrade Until Score", upgradeUntilScore),
            }.OfType<FieldChange>(),
        ];

    private static ProfileChange Create(
        QualityProfileIdentityResponse identity,
        IReadOnlyList<FieldChange> fields,
        ICollection<QualityProfileLayoutResponse> qualities,
        ICollection<QualityProfileScoreResponse> scores
    ) =>
        new(
            identity,
            "New",
            fields,
            [],
            [.. qualities],
            [
                .. scores.Select(x => new ScoreChange(
                    x.Name,
                    Unset,
                    FieldChange.Text(x.Score),
                    "Set"
                )),
            ]
        );

    private static ProfileChange Update(
        QualityProfileIdentityResponse identity,
        IReadOnlyList<FieldChange> fields,
        ValueChangeResponseOfIReadOnlyListOfQualityProfileLayoutResponse? layout,
        ICollection<QualityProfileScoreChangeResponse>? scores
    ) =>
        new(
            identity,
            "Changed",
            fields,
            [.. layout?.Current ?? []],
            [.. layout?.Desired ?? []],
            [
                .. (scores ?? []).Select(x => new ScoreChange(
                    x.Name,
                    FieldChange.Text(x.Value.Current),
                    FieldChange.Text(x.Value.Desired),
                    x.Reason.ToString()
                )),
            ]
        );
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
    IReadOnlyList<ScoreChange> Scores
);

internal sealed record ScoreChange(string Name, string Current, string Desired, string Reason);

/// <summary>
/// A labeled current-to-desired change of one settings field, with both values already formatted
/// for display.
/// </summary>
internal sealed record FieldChange(string Field, string Current, string Desired)
{
    public const string Unset = "UNSET";

    public static FieldChange? From(string field, ValueChangeResponseOfString? change) =>
        change is null ? null : new FieldChange(field, Text(change.Current), Text(change.Desired));

    public static FieldChange? From(string field, ValueChangeResponseOfNullableOfBoolean? change) =>
        change is null ? null : new FieldChange(field, Text(change.Current), Text(change.Desired));

    public static FieldChange? From(string field, ValueChangeResponseOfNullableOfInt32? change) =>
        change is null ? null : new FieldChange(field, Text(change.Current), Text(change.Desired));

    public static FieldChange? From(
        string field,
        ValueChangeResponseOfNullableOfPropersAndRepacksModeResponse? change
    ) => change is null ? null : new FieldChange(field, Text(change.Current), Text(change.Desired));

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
