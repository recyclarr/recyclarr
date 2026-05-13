using Recyclarr.Platform;

namespace Recyclarr.ErrorHandling;

internal class EnvironmentExceptionStrategy : IExceptionStrategy
{
    public Task<IReadOnlyList<string>?> HandleAsync(Exception exception)
    {
        return exception is not EnvironmentException
            ? Task.FromResult<IReadOnlyList<string>?>(null)
            : Task.FromResult<IReadOnlyList<string>?>([exception.Message]);
    }
}
