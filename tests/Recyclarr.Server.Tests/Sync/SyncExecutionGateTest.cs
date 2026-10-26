using Recyclarr.Server.Sync;
using Recyclarr.Server.Tests.Reusable;

namespace Recyclarr.Server.Tests.Sync;

internal sealed class SyncExecutionGateTest : ServerIntegrationFixture
{
    [Test]
    public async Task Second_job_is_admitted_only_after_the_first_exits()
    {
        var store = Resolve<ISyncJobStore>();
        var gate = new SyncExecutionGate();

        var first = await gate.EnterAsync(store.CreateManualJob([]), CancellationToken.None);
        var second = gate.EnterAsync(store.CreateManualJob([]), CancellationToken.None);

        // Admission is decided synchronously on entry and exit, so no wait is needed to observe it.
        second.IsCompleted.Should().BeFalse();
        first.Dispose();
        (await second.WaitAsync(TimeSpan.FromSeconds(10))).Dispose();
    }
}
