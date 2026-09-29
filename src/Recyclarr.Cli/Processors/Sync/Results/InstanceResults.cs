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
    QualityProfilePipelineResponse? QualityProfiles,
    QualitySizePipelineResponse? QualitySizes,
    NamingResults? Naming,
    MediaManagementPipelineResponse? MediaManagement
)
{
    public int CustomFormatChanges =>
        CustomFormats is { } x ? x.Creates.Count + x.Updates.Count + x.Deletes.Count : 0;

    public int QualityProfileChanges =>
        QualityProfiles is { } x ? x.Creates.Count + x.Updates.Count : 0;

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
                x.Pipelines.QualityProfiles,
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
                x.Pipelines.QualityProfiles,
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
