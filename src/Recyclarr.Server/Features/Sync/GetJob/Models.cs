using System.Text.Json.Serialization;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.Sync.GetJob;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GetSyncJobRequest
{
    public Guid Id { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record PipelineItemChangesResponse(
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Updated,
    IReadOnlyList<string> Deleted
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record PipelineSnapshotResponse(string Type, string Status)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Count { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PipelineItemChangesResponse? Changes { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record InstanceSnapshotResponse(
    string Name,
    string Status,
    IReadOnlyList<PipelineSnapshotResponse> Pipelines
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record DiagnosticEventResponse(string Level, string Message)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instance { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GetSyncJobResponse
{
    public required Guid Id { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SupportedServices? Service { get; init; }

    public required IReadOnlyCollection<string> Instances { get; init; }
    public required bool Preview { get; init; }
    public required IReadOnlyList<InstanceSnapshotResponse> Progress { get; init; }
    public required IReadOnlyList<DiagnosticEventResponse> Diagnostics { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConfigDiagnosticsResponse? ConfigDiagnostics { get; init; }
}
