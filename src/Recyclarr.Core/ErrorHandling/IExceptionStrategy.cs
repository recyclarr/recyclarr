namespace Recyclarr.ErrorHandling;

public interface IExceptionStrategy
{
    Task<HandledInstanceFailure?> HandleAsync(Exception exception);
}
