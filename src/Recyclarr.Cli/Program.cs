using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.ErrorHandling;

namespace Recyclarr.Cli;

internal static class Program
{
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Top level catch-all to translate exceptions; lack of specificity is intentional"
    )]
    public static async Task<int> Main(string[] args)
    {
        var builder = new ContainerBuilder();
        CompositionRoot.Setup(builder);
        var scope = builder.Build();

        try
        {
            return await CliSetup.Run(scope, args);
        }
        catch (Exception e)
        {
            var log = scope.Resolve<ILogger>();
            var exceptionHandler = new ConsoleExceptionHandler(log);
            if (!await exceptionHandler.HandleException(e))
            {
                log.Error(e, "Exiting due to fatal error");
            }

            return (int)ExitStatus.Failed;
        }
    }
}
