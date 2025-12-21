using Recyclarr.Cli.Console;
using Recyclarr.Compatibility;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.ErrorHandling.Strategies;

internal class ServiceExceptionStrategy : IExceptionStrategy
{
    public Task<IReadOnlyList<string>?> HandleAsync(Exception exception)
    {
        if (
            exception
            is not (ServiceIncompatibilityException or CommandException or CommandRuntimeException)
        )
        {
            return Task.FromResult<IReadOnlyList<string>?>(null);
        }

        return Task.FromResult<IReadOnlyList<string>?>([exception.Message]);
    }
}
