using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.MediaNaming;

internal sealed class RadarrNamingPipelineResultTest
{
    [Test]
    [Arguments(0, 0, SyncResultStatus.Succeeded)]
    [Arguments(1, 0, SyncResultStatus.Succeeded)]
    [Arguments(1, 1, SyncResultStatus.Partial)]
    [Arguments(0, 1, SyncResultStatus.Failed)]
    public void Status_derives_from_field_completion(
        int completed,
        int incomplete,
        SyncResultStatus expected
    )
    {
        var result = new RadarrNamingPipelineResult(completed, incomplete, [], null);

        result.Status.Should().Be(expected);
    }
}
