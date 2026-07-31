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
    public void Supports_terminal_status_with_optional_delta(SyncResultStatus status, bool hasDelta)
    {
        var delta = new MediaManagementDelta(
            new ValueDelta<PropersAndRepacksMode?>(
                PropersAndRepacksMode.PreferAndUpgrade,
                PropersAndRepacksMode.DoNotUpgrade
            )
        );

        var result = new MediaManagementPipelineResult(status, hasDelta ? delta : null);

        result.Status.Should().Be(status);
        if (hasDelta)
        {
            result.Delta.Should().BeEquivalentTo(delta);
        }
        else
        {
            result.Delta.Should().BeNull();
        }
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
