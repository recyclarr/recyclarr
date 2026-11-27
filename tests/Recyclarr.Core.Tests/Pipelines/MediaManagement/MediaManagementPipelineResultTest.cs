using Recyclarr.Config.Models;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.MediaManagement;

internal sealed class MediaManagementPipelineResultTest
{
    [Test]
    [Arguments(SyncResultStatus.Succeeded, true)]
    [Arguments(SyncResultStatus.Succeeded, false)]
    [Arguments(SyncResultStatus.Failed, true)]
    [Arguments(SyncResultStatus.Failed, false)]
    public void Supports_terminal_status_with_optional_settings(
        SyncResultStatus status,
        bool hasSettings
    )
    {
        var settings = new PlannedMediaManagementResource
        {
            Action = PlannedAction.Update,
            PropersAndRepacks = PlannedValue.Compared<PropersAndRepacksMode?>(
                PropersAndRepacksMode.PreferAndUpgrade,
                PropersAndRepacksMode.DoNotUpgrade
            ),
        };

        var result = new MediaManagementPipelineResult(status, hasSettings ? settings : null);

        result.Status.Should().Be(status);
        result.Settings.Should().Be(hasSettings ? settings : null);
    }

    [Test]
    [Arguments(SyncResultStatus.Partial)]
    [Arguments(SyncResultStatus.Blocked)]
    [Arguments((SyncResultStatus)999)]
    public void Rejects_unsupported_status(SyncResultStatus status)
    {
        var act = () => new MediaManagementPipelineResult(status, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
