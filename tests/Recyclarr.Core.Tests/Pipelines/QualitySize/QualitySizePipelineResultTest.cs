using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.QualitySize;

internal sealed class QualitySizePipelineResultTest
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
        var result = new QualitySizePipelineResult(completed, incomplete, [], []);

        result.Status.Should().Be(expected);
    }
}
