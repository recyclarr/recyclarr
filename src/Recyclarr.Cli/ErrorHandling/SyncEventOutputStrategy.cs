using Recyclarr.Sync.Events;

namespace Recyclarr.Cli.ErrorHandling;

internal class SyncEventOutputStrategy(ISyncEventPublisher publisher) : IErrorOutputStrategy
{
    public void WriteError(string message, Exception? exception = null)
    {
        publisher.AddError(message, exception);
    }
}
