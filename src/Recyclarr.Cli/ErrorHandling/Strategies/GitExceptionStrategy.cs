using Recyclarr.VersionControl;

namespace Recyclarr.Cli.ErrorHandling.Strategies;

internal class GitExceptionStrategy : IExceptionStrategy
{
    public Task<IReadOnlyList<string>?> HandleAsync(Exception exception)
    {
        if (exception is not GitCmdException e)
        {
            return Task.FromResult<IReadOnlyList<string>?>(null);
        }

        return Task.FromResult<IReadOnlyList<string>?>([
            $"Git command failed with exit code {e.ExitCode}",
        ]);
    }
}
