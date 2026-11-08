using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Sync.Results;

internal static class MediaManagementResponseMapper
{
    public static MediaManagementPipelineResponse ToResponse(
        MediaManagementPipelineResult result
    ) =>
        new()
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Settings = result.Settings is { } x ? MapSettings(x) : null,
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };

    private static MediaManagementSettingsResponse MapSettings(
        PlannedMediaManagementResource settings
    ) =>
        new(
            ResultValueMapper.MapAction(settings.Action),
            ResultValueMapper.MapValue(settings.PropersAndRepacks, MapMode)
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
