using System.ComponentModel;
using System.Text.Json.Serialization;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.Sync.GetJob;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GetSyncJobRequest
{
    /// <summary>The job ID from the create response or the job list.</summary>
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

/// <summary>
/// Why a scheduled job did not run: another job was active at its scheduled time.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SkippedBecauseResponse
{
    /// <summary>The ID of the job that was active.</summary>
    public required Guid JobId { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GetSyncJobResponse
{
    public required Guid Id { get; init; }

    /// <summary>
    /// `Pending` (queued) and `Running` are active; poll until the status changes. The final
    /// statuses are `Succeeded`, `Partial` (some changes were not applied), `Failed`,
    /// `Interrupted` (the server stopped before the job finished), and `Skipped` (a scheduled
    /// job that never ran because another job was active).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// `Manual` for a job created through the API or CLI, `Scheduled` for a job the built-in
    /// schedule created.
    /// </summary>
    public required string Trigger { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the job started to run; absent until then.</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>When the job reached a final status; absent until then.</summary>
    public DateTimeOffset? FinishedAt { get; init; }

    /// <summary>
    /// The schedule time a scheduled job was created for; absent for manual jobs.
    /// </summary>
    public DateTimeOffset? ScheduledFor { get; init; }

    /// <summary>Present only when the status is `Skipped`.</summary>
    public SkippedBecauseResponse? SkippedBecause { get; init; }

    /// <summary>The requested service filter; absent when the request had none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SupportedServices? Service { get; init; }

    /// <summary>The requested instance names; empty when the request selected all.</summary>
    public required IReadOnlyCollection<string> Instances { get; init; }

    /// <summary>True when the job computes changes without applying them.</summary>
    public required bool Preview { get; init; }

    /// <summary>
    /// One entry for each instance the job selected, updated as the job runs.
    /// </summary>
    public required IReadOnlyList<InstanceSnapshotResponse> Progress { get; init; }
}
