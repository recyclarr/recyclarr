using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaManagement;

/// <summary>
/// The terminal status and calculated change for media-management synchronization.
/// </summary>
public sealed record MediaManagementPipelineResult : PipelineResult
{
    internal MediaManagementPipelineResult(
        SyncResultStatus status,
        PlannedMediaManagementResource? settings
    )
        : this(ValidateStatus(status), null, settings) { }

    private MediaManagementPipelineResult(
        SyncResultStatus status,
        PipelineType? blockedBy,
        PlannedMediaManagementResource? settings
    )
        : base(status, blockedBy)
    {
        Settings = settings;
    }

    /// <summary>Absent when the pipeline did not compute.</summary>
    public PlannedMediaManagementResource? Settings { get; }

    internal override PipelineResult WithStatus(
        SyncResultStatus status,
        PipelineType? blockedBy = null
    ) => new MediaManagementPipelineResult(status, blockedBy, Settings);

    private static SyncResultStatus ValidateStatus(SyncResultStatus status) =>
        status is SyncResultStatus.Succeeded or SyncResultStatus.Failed
            ? status
            : throw new ArgumentOutOfRangeException(nameof(status));
}

public sealed record PlannedMediaManagementResource : PlannedResource
{
    public required PlannedValue<PropersAndRepacksMode?> PropersAndRepacks { get; init; }
}
