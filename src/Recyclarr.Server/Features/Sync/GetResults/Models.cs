using System.Text.Json.Serialization;

namespace Recyclarr.Server.Features.Sync.GetResults;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GetSyncJobResultsRequest
{
    public Guid Id { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SyncJobResultsResponse
{
    public required Guid Id { get; init; }
    public required SyncCompletionStatus Status { get; init; }
    public required IReadOnlyList<SyncInstanceResultsResponse> Instances { get; init; }
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

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SyncFaultResponse(string Reference);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "service")]
[JsonDerivedType(typeof(SonarrInstanceResultsResponse), "sonarr")]
[JsonDerivedType(typeof(RadarrInstanceResultsResponse), "radarr")]
internal abstract record SyncInstanceResultsResponse
{
    public required string Name { get; init; }
    public required SyncCompletionStatus Status { get; init; }
    public InstanceFailureCategory? Failure { get; init; }
    public SyncFaultResponse? Fault { get; init; }
    public IReadOnlyList<PlanningOutcomeResponse> PlanningOutcomes { get; init; } = [];
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SonarrInstanceResultsResponse : SyncInstanceResultsResponse
{
    public required SonarrPipelinesResponse Pipelines { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record RadarrInstanceResultsResponse : SyncInstanceResultsResponse
{
    public required RadarrPipelinesResponse Pipelines { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SonarrPipelinesResponse
{
    public CustomFormatPipelineResponse? CustomFormats { get; init; }
    public SonarrQualityProfilePipelineResponse? QualityProfiles { get; init; }
    public QualitySizePipelineResponse? QualitySizes { get; init; }
    public SonarrNamingPipelineResponse? Naming { get; init; }
    public MediaManagementPipelineResponse? MediaManagement { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record RadarrPipelinesResponse
{
    public CustomFormatPipelineResponse? CustomFormats { get; init; }
    public RadarrQualityProfilePipelineResponse? QualityProfiles { get; init; }
    public QualitySizePipelineResponse? QualitySizes { get; init; }
    public RadarrNamingPipelineResponse? Naming { get; init; }
    public MediaManagementPipelineResponse? MediaManagement { get; init; }
}
