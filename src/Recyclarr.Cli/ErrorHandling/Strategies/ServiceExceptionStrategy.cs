using Recyclarr.Common;
using Recyclarr.Compatibility;
using Recyclarr.ErrorHandling;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.ErrorHandling.Strategies;

internal class ServiceExceptionStrategy : IExceptionStrategy
{
    public Task<HandledInstanceFailure?> HandleAsync(Exception exception)
    {
        return
            exception
                is not (
                    ServiceIncompatibilityException
                    or CommandException
                    or CommandRuntimeException
                )
            ? Task.FromResult<HandledInstanceFailure?>(null)
            : Task.FromResult<HandledInstanceFailure?>(new ServiceFailure(exception.Message));
    }
}
