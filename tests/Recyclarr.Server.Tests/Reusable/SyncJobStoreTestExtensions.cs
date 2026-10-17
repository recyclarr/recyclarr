using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Tests.Reusable;

/// <summary>
/// Creates jobs with defaults for what most tests ignore, so a new parameter on
/// <see cref="ISyncJobStore.Create"/> changes this helper instead of every test.
/// </summary>
internal static class SyncJobStoreTestExtensions
{
    private static readonly ServerSyncSettings AllInstances = new(null, [], Preview: false);

    extension(ISyncJobStore store)
    {
        public SyncJob CreateManualJob(
            IReadOnlyList<string> instanceNames,
            ServerSyncSettings? request = null
        )
        {
            return store.Create(request ?? AllInstances, instanceNames, traceParent: null);
        }
    }
}
