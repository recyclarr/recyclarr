using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.CustomFormat;

internal sealed class CustomFormatPipelineResultTest
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
        var result = new CustomFormatPipelineResult(completed, incomplete, [], []);

        result.Status.Should().Be(expected);
    }
}
