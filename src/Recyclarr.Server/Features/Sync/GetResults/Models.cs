namespace Recyclarr.Server.Features.Sync.GetResults;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GetSyncJobResultsRequest
{
    public Guid Id { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SyncJobResultsResponse(
    Guid Id,
    SyncCompletionStatus Status,
    IReadOnlyList<SyncInstanceResultsResponse> Instances
)
{
    public SyncFaultResponse? Fault { get; init; }
}

internal enum SyncCompletionStatus
{
    Succeeded,
    Partial,
    Failed,
}

internal enum PipelineStatus
{
    Succeeded,
    Partial,
    Failed,
    Blocked,
}

internal enum BlockingPipeline
{
    CustomFormat,
    QualityProfile,
    QualitySize,
    MediaNaming,
    MediaManagement,
}

internal enum InstanceFailureCategory
{
    ServiceUnavailable,
    ServiceUnauthenticated,
    ServiceUnauthorized,
    ServiceRateLimited,
    ServiceIncompatible,
    SyncStateUnavailable,
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SyncFaultResponse(string Reference);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal abstract record SyncInstanceResultsResponse(string Name, SyncCompletionStatus Status)
{
    public InstanceFailureCategory? Failure { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SonarrInstanceResultsResponse(
    string Name,
    SyncCompletionStatus Status,
    SonarrPipelinesResponse Pipelines
) : SyncInstanceResultsResponse(Name, Status);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record RadarrInstanceResultsResponse(
    string Name,
    SyncCompletionStatus Status,
    RadarrPipelinesResponse Pipelines
) : SyncInstanceResultsResponse(Name, Status);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SonarrPipelinesResponse
{
    public CustomFormatPipelineResponse? CustomFormats { get; init; }
    public QualityProfilePipelineResponse? QualityProfiles { get; init; }
    public QualitySizePipelineResponse? QualitySizes { get; init; }
    public SonarrNamingPipelineResponse? Naming { get; init; }
    public MediaManagementPipelineResponse? MediaManagement { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record RadarrPipelinesResponse
{
    public CustomFormatPipelineResponse? CustomFormats { get; init; }
    public QualityProfilePipelineResponse? QualityProfiles { get; init; }
    public QualitySizePipelineResponse? QualitySizes { get; init; }
    public RadarrNamingPipelineResponse? Naming { get; init; }
    public MediaManagementPipelineResponse? MediaManagement { get; init; }
}
