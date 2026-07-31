using Recyclarr.VersionControl;

namespace Recyclarr.ErrorHandling;

internal class GitExceptionStrategy : IExceptionStrategy
{
    public Task<HandledInstanceFailure?> HandleAsync(Exception exception)
    {
        if (exception is not GitCmdException e)
        {
            return Task.FromResult<HandledInstanceFailure?>(null);
        }

        return Task.FromResult<HandledInstanceFailure?>(new GitFailure(e.ExitCode));
    }
}
