using Recyclarr.Sync;

namespace Recyclarr.Server.Sync;

internal sealed class ServerSyncFaultReporter(ILogger log) : ISyncFaultReporter
{
    public void Report(string reference, Exception exception)
    {
        log.Error(exception, "Unexpected sync fault {Reference}", reference);
    }
}
