namespace Recyclarr.Server.Features.Sync.GetResults;

internal enum PropersAndRepacksModeResponse
{
    PreferAndUpgrade,
    DoNotUpgrade,
    DoNotPrefer,
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record MediaManagementUpdateResponse(
    ValueChangeResponse<PropersAndRepacksModeResponse?> PropersAndRepacks
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record MediaManagementPipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required IReadOnlyList<MediaManagementUpdateResponse> Updates { get; init; }
    public BlockingPipeline? BlockedBy { get; init; }
}
