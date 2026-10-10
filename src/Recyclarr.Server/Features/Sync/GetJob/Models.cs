using System.ComponentModel;
using System.Text.Json.Serialization;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.Sync.GetJob;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GetSyncJobRequest
{
    public Guid Id { get; init; }
}

/// <summary>Sync progress of one instance in a job.</summary>
internal enum InstanceProgressStatusResponse
{
    [Description("The instance has not started yet.")]
    Pending,

    [Description("The instance is syncing now.")]
    Running,

    [Description("The instance finished and every change was applied.")]
    Succeeded,

    [Description("The instance finished, but some changes were not applied. See the job results.")]
    Partial,

    [Description("The instance finished without its changes. See the job results.")]
    Failed,

    [Description("The job stopped while this instance was syncing.")]
    Interrupted,

    [Description("The job stopped before this instance started.")]
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
