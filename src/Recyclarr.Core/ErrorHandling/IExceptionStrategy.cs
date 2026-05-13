namespace Recyclarr.ErrorHandling;

public interface IExceptionStrategy
{
    Task<IReadOnlyList<string>?> HandleAsync(Exception exception);
}
