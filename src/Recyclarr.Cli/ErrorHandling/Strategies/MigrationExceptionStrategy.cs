using Recyclarr.Cli.Migration;

namespace Recyclarr.Cli.ErrorHandling.Strategies;

internal class MigrationExceptionStrategy : IExceptionStrategy
{
    public Task<IReadOnlyList<string>?> HandleAsync(Exception exception)
    {
        if (exception is not MigrationException e)
        {
            return Task.FromResult<IReadOnlyList<string>?>(null);
        }

        List<string> messages =
        [
            $"Migration step failed: {e.OperationDescription}",
            $"Reason: {e.OriginalException.Message}",
            .. e.Remediation.Select(r => $"  - {r}"),
        ];

        return Task.FromResult<IReadOnlyList<string>?>(messages);
    }
}
