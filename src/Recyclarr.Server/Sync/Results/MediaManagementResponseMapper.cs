using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Sync.Results;

internal static class MediaManagementResponseMapper
{
    public static MediaManagementPipelineResponse ToResponse(
        MediaManagementPipelineResult result
    ) =>
        new(
            ResultValueMapper.MapStatus(result.Status),
            result.Delta is null ? [] : [MapUpdate(result.Delta)]
        )
        {
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };

    private static MediaManagementUpdateResponse MapUpdate(MediaManagementDelta delta) =>
        new(
            new ValueChangeResponse<PropersAndRepacksModeResponse?>(
                MapMode(delta.PropersAndRepacks.Current),
                MapMode(delta.PropersAndRepacks.Desired)
            )
        );

    private static PropersAndRepacksModeResponse? MapMode(
        Config.Models.PropersAndRepacksMode? mode
    ) =>
        mode switch
        {
            null => null,
            Config.Models.PropersAndRepacksMode.PreferAndUpgrade =>
                PropersAndRepacksModeResponse.PreferAndUpgrade,
            Config.Models.PropersAndRepacksMode.DoNotUpgrade =>
                PropersAndRepacksModeResponse.DoNotUpgrade,
            Config.Models.PropersAndRepacksMode.DoNotPrefer =>
                PropersAndRepacksModeResponse.DoNotPrefer,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };
}
