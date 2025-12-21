namespace Recyclarr.Cli.ErrorHandling;

internal class LogOnlyOutputStrategy(ILogger log) : IErrorOutputStrategy
{
    public void WriteError(string message, Exception? exception = null)
    {
        log.Error(exception, "{Message}", message);
    }
}
