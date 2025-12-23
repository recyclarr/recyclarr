using Recyclarr.Sync.Events;

namespace Recyclarr.Cli.ErrorHandling;

internal class SyncEventOutputStrategy(ISyncEventPublisher publisher) : IErrorOutputStrategy
{
    public void WriteError(string message)
    {
        publisher.AddError(message);
    }
}
