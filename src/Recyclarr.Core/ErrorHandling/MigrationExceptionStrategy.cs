using Recyclarr.Migration;

namespace Recyclarr.ErrorHandling;

internal class MigrationExceptionStrategy : IExceptionStrategy
{
    public Task<HandledInstanceFailure?> HandleAsync(Exception exception)
    {
        if (exception is not MigrationException e)
        {
            return Task.FromResult<HandledInstanceFailure?>(null);
        }

        return Task.FromResult<HandledInstanceFailure?>(
            new MigrationFailure(
                e.OperationDescription,
                e.OriginalException.Message,
                e.Remediation.ToList()
            )
        );
    }
}
