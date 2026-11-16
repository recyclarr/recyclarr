using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.TestLibrary;

/// <summary>
/// Builds terminal pipeline results for tests outside Recyclarr.Core, whose result constructors
/// are internal. Each resource counts as completed work; <c>failed</c> counts incomplete work.
/// </summary>
public static class NewPipelineResult
{
    public static PipelineResult CustomFormats(
        IReadOnlyList<CustomFormatOutcome> outcomes,
        IReadOnlyList<PlannedCustomFormatResource> resources,
        IReadOnlyList<CustomFormatIdentity>? deletes = null,
        int failed = 0
    ) =>
        new CustomFormatPipelineResult(
            resources.Count + (deletes?.Count ?? 0),
            failed,
            outcomes,
            resources,
            deletes ?? []
        );

    public static PipelineResult QualityProfiles(
        IReadOnlyList<QualityProfileOutcome> outcomes,
        IReadOnlyList<PlannedQualityProfileResource> resources,
        int failed = 0
    ) => new QualityProfilePipelineResult(resources.Count, failed, outcomes, resources);

    public static PipelineResult QualitySizes(
        IReadOnlyList<QualitySizeOutcome> outcomes,
        IReadOnlyList<PlannedQualitySizeResource> resources,
        int failed = 0
    ) => new QualitySizePipelineResult(resources.Count, failed, outcomes, resources);

    public static PipelineResult RadarrNaming(
        PlannedRadarrNamingResource? settings,
        IReadOnlyList<RadarrNamingOutcome>? outcomes = null
    ) =>
        new RadarrNamingPipelineResult(
            completedFields: 1,
            incompleteFields: outcomes?.Count ?? 0,
            outcomes ?? [],
            settings
        );

    public static PipelineResult MediaManagement(PlannedMediaManagementResource? settings) =>
        new MediaManagementPipelineResult(SyncResultStatus.Succeeded, settings);

    public static PipelineResult Blocked(PipelineResult result, PipelineType blockedBy) =>
        result.WithStatus(SyncResultStatus.Blocked, blockedBy);
}
