namespace Recyclarr.Cli.ErrorHandling;

internal interface IErrorOutputStrategy
{
    void Write(IReadOnlyList<string> messages, Exception exception);
}
