using System.Text.Json.Serialization;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.Sync.GetJob;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GetSyncJobRequest
{
    public Guid Id { get; init; }
}

internal enum InstanceProgressStatusResponse
{
    Pending,
    Running,
    Succeeded,
    Partial,
    Failed,
    Interrupted,
    NotRun,
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record InstanceSnapshotResponse(string Name, InstanceProgressStatusResponse Status);

// Why a scheduled occurrence did not run: the job that was active at the time.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SkippedBecauseResponse(Guid JobId);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GetSyncJobResponse
{
    public required Guid Id { get; init; }
    public required string Status { get; init; }
    public required string Trigger { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }

    // The schedule occurrence a scheduled job was created for.
    public DateTimeOffset? ScheduledFor { get; init; }

    public SkippedBecauseResponse? SkippedBecause { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SupportedServices? Service { get; init; }

    public required IReadOnlyCollection<string> Instances { get; init; }
    public required bool Preview { get; init; }
    public required IReadOnlyList<InstanceSnapshotResponse> Progress { get; init; }
}
