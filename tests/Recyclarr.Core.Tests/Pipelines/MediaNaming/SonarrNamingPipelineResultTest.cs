using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.MediaNaming;

internal sealed class SonarrNamingPipelineResultTest
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
        var result = new SonarrNamingPipelineResult(completed, incomplete, [], null);

        result.Status.Should().Be(expected);
    }
}
