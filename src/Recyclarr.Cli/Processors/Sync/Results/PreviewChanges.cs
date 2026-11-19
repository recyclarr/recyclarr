using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync.Results;

/// <summary>
/// Reduces one instance's results to <see cref="PreviewPipeline"/>s, one per pipeline that ran
/// or was planned, so the renderer draws every resource type the same way. Pipelines absent from
/// the results (not configured, or not applicable such as instance-wide sizes on Sonarr v5) are
/// omitted.
/// </summary>
internal static class PreviewChanges
{
    public static IReadOnlyList<PreviewPipeline> Of(InstanceResults instance)
    {
        var pipelines = new List<PreviewPipeline>();

        if (instance.CustomFormats is { } cf)
        {
            pipelines.Add(
                Pipeline(PipelineNames.CustomFormats, cf.Status, cf.BlockedBy, CustomFormats(cf))
            );
        }

        if (instance.QualityProfiles is { } qp)
        {
            pipelines.Add(
                Pipeline(
                    PipelineNames.QualityProfiles,
                    qp.Status,
                    qp.BlockedBy,
                    [.. qp.Changes.Select(Profile)]
                )
            );
        }

        if (instance.QualitySizes is { } qs)
        {
            pipelines.Add(
                Pipeline(PipelineNames.QualitySizes, qs.Status, qs.BlockedBy, QualitySizes(qs))
            );
        }

        if (instance.Naming is { } naming)
        {
            IReadOnlyList<PreviewChange> changes =
                naming.Changes.Count == 0 ? [] : [Item(naming.Subject, naming.Changes)];

            pipelines.Add(
                Pipeline(PipelineNames.MediaNaming, naming.Status, naming.BlockedBy, changes)
            );
        }

        if (instance.MediaManagement is { } mm)
        {
            var fields = new[]
            {
                FieldChange.From("Download Propers and Repacks", mm.Settings?.PropersAndRepacks),
            }
                .OfType<FieldChange>()
                .ToList();

            IReadOnlyList<PreviewChange> changes =
                fields.Count == 0 ? [] : [Item("Settings", fields)];
            pipelines.Add(
                Pipeline(PipelineNames.MediaManagement, mm.Status, mm.BlockedBy, changes)
            );
        }

        return pipelines;
    }

    private static PreviewPipeline Pipeline(
        string title,
        PipelineStatus status,
        BlockingPipeline? blockedBy,
        IReadOnlyList<PreviewChange> changes
    )
    {
        if (blockedBy is { } blocker)
        {
            return new PreviewPipeline(
                title,
                [],
                new PreviewSkip("not run", $"blocked by {PipelineNames.Of(blocker)}")
            );
        }

        var skip =
            status == PipelineStatus.Failed && changes.Count == 0
                ? new PreviewSkip("failed", "see diagnostics")
                : null;

        return new PreviewPipeline(title, changes, skip);
    }

    private static PreviewChange Item(string name, IReadOnlyList<FieldChange> fields) =>
        new(ChangeKind.Update, name, null, fields, []);

    private static ChangeKind Kind(ResourceAction action) =>
        action == ResourceAction.Create ? ChangeKind.Create : ChangeKind.Update;

    private static List<PreviewChange> CustomFormats(CustomFormatPipelineResponse pipeline) =>
        [
            .. pipeline
                .Items.Where(InstanceResults.IsChanged)
                .OrderBy(x => Kind(x.Action))
                .Select(x => new PreviewChange(
                    Kind(x.Action),
                    x.Identity.Name,
                    Source(x.SelectionProvenance),
                    [],
                    []
                )),
            .. pipeline.Deletes.Select(x => new PreviewChange(
                ChangeKind.Delete,
                x.Name,
                "no longer in config",
                [],
                []
            )),
        ];

    private static string Source(CustomFormatSelectionResponse selection)
    {
        var profiles =
            (selection.ProfileNames.Count == 1 ? "profile " : "profiles ")
            + string.Join(", ", selection.ProfileNames);

        var group = selection.InclusionReason switch
        {
            CustomFormatInclusionReason.Required => $"from group {selection.GroupName}, required",
            CustomFormatInclusionReason.Default => $"from group {selection.GroupName}, default",
            CustomFormatInclusionReason.Selected => $"from group {selection.GroupName}, selected",
            _ => $"from group {selection.GroupName}",
        };

        return selection.Source switch
        {
            CustomFormatSource.ProfileFormatItems => $"from {profiles}",
            CustomFormatSource.CfGroupImplicit => $"{group} (via {profiles})",
            CustomFormatSource.CfGroupExplicit => group,
            _ => "from custom_formats",
        };
    }

    private static PreviewChange Profile(ProfileChange change)
    {
        PreviewSection?[] sections =
        [
            Section("Settings", change.Fields),
            change.DesiredQualities.Count == 0
                ? null
                : new PreviewSection(
                    "Qualities",
                    null,
                    [],
                    QualityLayoutDiff.Lines(change.CurrentQualities, change.DesiredQualities)
                ),
            Section("Scores", change.Scores),
            Section("Quality Sizes", change.Sizes, SizesSource(change.SizesSource)),
        ];

        return new PreviewChange(
            Kind(change.Action),
            change.Identity.Name,
            null,
            [],
            [.. sections.OfType<PreviewSection>()]
        );

        static PreviewSection? Section(
            string title,
            IReadOnlyList<FieldChange> fields,
            string? note = null
        ) => fields.Count == 0 ? null : new PreviewSection(title, note, fields, []);
    }

    // Unknown values come from a newer server; showing nothing beats guessing.
    private static string? SizesSource(string? source) =>
        source switch
        {
            "profile" => "from profile quality_definition",
            "guide_profile" => "from guide profile",
            "instance" => "from instance quality_definition",
            _ => null,
        };

    private static List<PreviewChange> QualitySizes(QualitySizePipelineResponse pipeline) =>
        [
            .. pipeline
                .Items.Where(InstanceResults.IsChanged)
                .Select(x => new PreviewChange(
                    Kind(x.Action),
                    x.Quality,
                    null,
                    [.. QualitySizeText.Fields(x, "")],
                    []
                )),
        ];
}
