using Recyclarr.Platform;

namespace Recyclarr.ErrorHandling;

internal class EnvironmentExceptionStrategy : IExceptionStrategy
{
    public Task<HandledInstanceFailure?> HandleAsync(Exception exception)
    {
        return exception is not EnvironmentException
            ? Task.FromResult<HandledInstanceFailure?>(null)
            : Task.FromResult<HandledInstanceFailure?>(new EnvironmentFailure(exception.Message));
    }
}
