using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.QualityProfile;

internal sealed class QualityProfilePipelineResultTest
{
    [TestCase(0, 0, SyncResultStatus.Succeeded)]
    [TestCase(1, 0, SyncResultStatus.Succeeded)]
    [TestCase(1, 1, SyncResultStatus.Partial)]
    [TestCase(0, 1, SyncResultStatus.Failed)]
    public void Status_derives_from_resource_completion(
        int completed,
        int incomplete,
        SyncResultStatus expected
    )
    {
        var result = new QualityProfilePipelineResult(completed, incomplete, [], []);

        result.Status.Should().Be(expected);
    }
}
