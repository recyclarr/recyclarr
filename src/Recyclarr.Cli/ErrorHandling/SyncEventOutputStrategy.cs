using Recyclarr.Sync;

namespace Recyclarr.Cli.ErrorHandling;

internal class SyncEventOutputStrategy(IInstancePublisher publisher) : IErrorOutputStrategy
{
    public void WriteError(string message)
    {
        publisher.AddError(message);
    }
}
