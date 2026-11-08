namespace Recyclarr.Server.Features.Sync.GetResults;

internal enum PropersAndRepacksModeResponse
{
    PreferAndUpgrade,
    DoNotUpgrade,
    DoNotPrefer,
}

internal sealed record MediaManagementSettingsResponse(
    ResourceAction Action,
    PlannedValueResponse<PropersAndRepacksModeResponse?> PropersAndRepacks
);

internal sealed record MediaManagementPipelineResponse
{
    public required PipelineStatus Status { get; init; }

    /// <summary>Absent when the pipeline did not run.</summary>
    public MediaManagementSettingsResponse? Settings { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}
