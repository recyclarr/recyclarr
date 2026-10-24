using System.ComponentModel;
using System.Text.Json.Serialization;
using Recyclarr.Server.Api;

namespace Recyclarr.Server.Features.Sync.GetResults;

internal sealed record GetSyncJobResultsRequest
{
    /// <summary>The job ID from the create response or the job list.</summary>
    public Guid Id { get; init; }
}

internal sealed record SyncJobResultsResponse
{
    public required Guid Id { get; init; }

    /// <summary>
    /// The overall result. An interrupted job reports `partial` when any instance succeeded or
    /// partly succeeded, otherwise `failed`.
    /// </summary>
    public required SyncCompletionStatus Status { get; init; }

    /// <summary>
    /// One entry for each instance that finished; instances that never ran are absent.
    /// </summary>
    public required IReadOnlyList<SyncInstanceResultsResponse> Instances { get; init; }

    /// <summary>Present when an unexpected error stopped the job.</summary>
    public SyncFaultResponse? Fault { get; init; }
}

/// <summary>The result of a job or of one instance.</summary>
internal enum SyncCompletionStatus
{
    [Description("Every item synced.")]
    Succeeded,

    [Description("Some items synced and some did not.")]
    Partial,

    [Description("No items synced.")]
    Failed,
}

/// <summary>The result of one pipeline (for example custom formats) for one instance.</summary>
internal enum PipelineStatus
{
    [Description("Every item synced. A pipeline with nothing to change also succeeds.")]
    Succeeded,

    [Description("Some items synced and some did not. The outcomes tell why.")]
    Partial,

    [Description("No items synced. The outcomes tell why.")]
    Failed,

    [Description("Did not run because a pipeline it depends on did not succeed. See `blockedBy`.")]
    Blocked,
}

/// <summary>
/// The pipeline that blocked this one. Quality profiles depend on custom formats, because scores
/// need every custom format to exist.
/// </summary>
internal enum BlockingPipeline
{
    CustomFormat,
    QualityProfile,
    QualitySize,
    MediaNaming,
    MediaManagement,
}

/// <summary>An unexpected error. The server log has the details under the same reference.</summary>
internal sealed record SyncFaultResponse(string Reference);

/// <summary>
/// The result of one instance. The `service` property selects the pipeline shape.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "service")]
[JsonDerivedType(typeof(SonarrInstanceResultsResponse), "sonarr")]
[JsonDerivedType(typeof(RadarrInstanceResultsResponse), "radarr")]
internal abstract record SyncInstanceResultsResponse
{
    public required string Name { get; init; }
    public required SyncCompletionStatus Status { get; init; }

    /// <summary>
    /// Present when a call to Sonarr or Radarr failed and stopped the instance. Pipelines that
    /// finished before the failure keep their results.
    /// </summary>
    public InstanceFailureCategory? Failure { get; init; }

    /// <summary>Present when an unexpected error stopped the instance.</summary>
    public SyncFaultResponse? Fault { get; init; }

    /// <summary>
    /// Configuration problems found before any pipeline ran. A blocking outcome stops every
    /// pipeline of the instance.
    /// </summary>
    public IReadOnlyList<PlanningOutcomeResponse> PlanningOutcomes { get; init; } = [];
}

internal sealed record SonarrInstanceResultsResponse : SyncInstanceResultsResponse
{
    public required SonarrPipelinesResponse Pipelines { get; init; }
}

internal sealed record RadarrInstanceResultsResponse : SyncInstanceResultsResponse
{
    public required RadarrPipelinesResponse Pipelines { get; init; }
}

/// <summary>
/// One result for each pipeline. A pipeline is absent when the configuration does not use it or
/// the instance stopped before it ran.
/// </summary>
internal sealed record SonarrPipelinesResponse
{
    public CustomFormatPipelineResponse? CustomFormats { get; init; }
    public SonarrQualityProfilePipelineResponse? QualityProfiles { get; init; }
    public QualitySizePipelineResponse? QualitySizes { get; init; }
    public SonarrNamingPipelineResponse? Naming { get; init; }
    public MediaManagementPipelineResponse? MediaManagement { get; init; }
}

/// <summary>
/// One result for each pipeline. A pipeline is absent when the configuration does not use it or
/// the instance stopped before it ran.
/// </summary>
internal sealed record RadarrPipelinesResponse
{
    public CustomFormatPipelineResponse? CustomFormats { get; init; }
    public RadarrQualityProfilePipelineResponse? QualityProfiles { get; init; }
    public QualitySizePipelineResponse? QualitySizes { get; init; }
    public RadarrNamingPipelineResponse? Naming { get; init; }
    public MediaManagementPipelineResponse? MediaManagement { get; init; }
}
