namespace Recyclarr.Server.Features.Sync.GetResults;

internal enum PropersAndRepacksModeResponse
{
    PreferAndUpgrade,
    DoNotUpgrade,
    DoNotPrefer,
}

internal sealed record MediaManagementUpdateResponse(
    ValueChangeResponse<PropersAndRepacksModeResponse?> PropersAndRepacks
);

internal sealed record MediaManagementPipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required IReadOnlyList<MediaManagementUpdateResponse> Updates { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}
