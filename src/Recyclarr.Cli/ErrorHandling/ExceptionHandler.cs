using Autofac.Core;

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
            var messages = await strategy.HandleAsync(actualException);
            if (messages is null)
            {
                continue;
            }

            var outputStrategy = output ?? defaultOutput;
            foreach (var message in messages)
            {
                outputStrategy.WriteError(message);
            }

            return true;
        }

        return false;
    }
}
