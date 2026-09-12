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
internal sealed record MediaManagementPipelineResponse(
    PipelineStatus Status,
    IReadOnlyList<MediaManagementUpdateResponse> Updates
)
{
    public BlockingPipeline? BlockedBy { get; init; }
}
