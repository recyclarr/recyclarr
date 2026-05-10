using Recyclarr.Sync;

namespace Recyclarr.Cli.ErrorHandling;

internal class SyncEventOutputStrategy(IInstancePublisher publisher, ILogger log)
    : IErrorOutputStrategy
{
    public void Write(IReadOnlyList<string> messages, Exception exception)
    {
        foreach (var message in messages)
        {
            publisher.AddError(message);
        }

        log.Debug(exception, "Instance sync error (details logged for diagnostics)");
    }
}
