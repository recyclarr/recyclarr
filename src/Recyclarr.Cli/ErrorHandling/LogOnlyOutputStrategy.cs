namespace Recyclarr.Cli.ErrorHandling;

internal class LogOnlyOutputStrategy(ILogger log) : IErrorOutputStrategy
{
    public void WriteError(string message)
    {
        log.Error("{Message}", message);
    }
}
