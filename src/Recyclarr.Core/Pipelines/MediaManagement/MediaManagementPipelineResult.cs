using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaManagement;

/// <summary>
/// The terminal status and calculated change for media-management synchronization.
/// </summary>
public sealed record MediaManagementPipelineResult : PipelineResult
{
    internal MediaManagementPipelineResult(SyncResultStatus status, MediaManagementDelta? delta)
        : this(ValidateStatus(status), null, delta) { }

    private MediaManagementPipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        MediaManagementDelta? delta
    )
        : base(status, blockedBy)
    {
        Delta = delta;
    }

    public MediaManagementDelta? Delta { get; }

    internal override PipelineResult WithStatus(
        SyncResultStatus status,
        PipelineType? blockedBy = null
    ) => new MediaManagementPipelineResult(status, blockedBy, Delta);

    private static SyncResultStatus ValidateStatus(SyncResultStatus status) =>
        status is SyncResultStatus.Succeeded or SyncResultStatus.Failed
            ? status
            : throw new ArgumentOutOfRangeException(nameof(status));
}

public sealed record MediaManagementDelta(ValueDelta<PropersAndRepacksMode?> PropersAndRepacks)
    : ResourceDelta;
