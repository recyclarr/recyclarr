using Autofac.Core;
using Recyclarr.ErrorHandling;
using Recyclarr.Sync;

namespace Recyclarr.Cli.ErrorHandling;

internal class ExceptionHandler(
    IEnumerable<IExceptionStrategy> strategies,
    FatalErrorOutputStrategy defaultOutput
)
{
    public async Task<bool> TryHandleAsync(Exception exception, IErrorOutputStrategy? output = null)
    {
        // Unwrap DI exceptions to get the actual cause
        var actualException = exception
            is DependencyResolutionException { InnerException: { } inner }
            ? inner
            : exception;

        foreach (var strategy in strategies)
        {
            var failure = await strategy.HandleAsync(actualException);
            if (failure is null)
            {
                continue;
            }

            (output ?? defaultOutput).Write(SyncOutcomeFormatter.Format(failure), actualException);
            return true;
        }

        return false;
    }
}
