using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.TestLibrary;

/// <summary>
/// Builds terminal pipeline results for tests outside Recyclarr.Core, whose result constructors
/// are internal. Each change counts as completed work; <c>failed</c> counts incomplete work.
/// </summary>
public static class NewPipelineResult
{
    public static PipelineResult CustomFormats(
        IReadOnlyList<CustomFormatOutcome> outcomes,
        IReadOnlyList<CustomFormatDelta> deltas,
        int failed = 0
    ) => new CustomFormatPipelineResult(deltas.Count, failed, outcomes, deltas);

    public static PipelineResult QualityProfiles(
        IReadOnlyList<QualityProfileOutcome> outcomes,
        IReadOnlyList<QualityProfileDelta> deltas,
        int failed = 0
    ) => new QualityProfilePipelineResult(deltas.Count, failed, outcomes, deltas);

    public static PipelineResult QualitySizes(
        IReadOnlyList<QualitySizeOutcome> outcomes,
        IReadOnlyList<QualitySizeDelta> deltas,
        int failed = 0
    ) => new QualitySizePipelineResult(deltas.Count, failed, outcomes, deltas);

    public static PipelineResult RadarrNaming(
        RadarrNamingDelta? delta,
        IReadOnlyList<RadarrNamingOutcome>? outcomes = null
    ) =>
        new RadarrNamingPipelineResult(
            completedFields: 1,
            incompleteFields: outcomes?.Count ?? 0,
            outcomes ?? [],
            delta
        );

    public static PipelineResult MediaManagement(MediaManagementDelta? delta) =>
        new MediaManagementPipelineResult(SyncResultStatus.Succeeded, delta);

    public static PipelineResult Blocked(PipelineResult result, PipelineType blockedBy) =>
        result.WithStatus(SyncResultStatus.Blocked, blockedBy);
}
