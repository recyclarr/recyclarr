namespace Recyclarr.Cli.ErrorHandling;

internal interface IExceptionStrategy
{
    Task<IReadOnlyList<string>?> HandleAsync(Exception exception);
}
