namespace Recyclarr.Cli.ErrorHandling;

internal interface IErrorOutputStrategy
{
    void WriteError(string message, Exception? exception = null);
}
