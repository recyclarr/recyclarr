using DotNext.Threading;

namespace Recyclarr.Server.Resources;

/// <summary>
/// A read hold on provider data. While any lease is held, no refresh changes provider files, so
/// the holder can read every file its snapshot lists. Dispose releases the hold.
/// </summary>
internal sealed class ResourceLease(AsyncReaderWriterLock owner, ResourceSnapshot snapshot)
    : IDisposable
{
    private int _released;

    public ResourceSnapshot Snapshot => snapshot;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _released, 1) == 0)
        {
            owner.Release();
        }
    }
}
