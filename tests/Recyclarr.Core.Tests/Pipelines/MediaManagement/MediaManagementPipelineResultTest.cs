using Recyclarr.Config.Models;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.MediaManagement;

internal sealed class MediaManagementPipelineResultTest
{
    [TestCase(SyncResultStatus.Succeeded, true)]
    [TestCase(SyncResultStatus.Succeeded, false)]
    [TestCase(SyncResultStatus.Failed, true)]
    [TestCase(SyncResultStatus.Failed, false)]
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

    [TestCase(SyncResultStatus.Partial)]
    [TestCase(SyncResultStatus.Blocked)]
    [TestCase((SyncResultStatus)999)]
    public void Rejects_unsupported_status(SyncResultStatus status)
    {
        var act = () => new MediaManagementPipelineResult(status, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
