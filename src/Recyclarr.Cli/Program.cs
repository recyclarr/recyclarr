using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Processors;
using Spectre.Console;

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
            scope.Resolve<MigrationExecutor>().PerformAllMigrationSteps();
            return await CliSetup.Run(scope, args);
        }
        catch (Exception e)
        {
            var exceptionHandler = scope.Resolve<ExceptionHandler>();
            if (!await exceptionHandler.TryHandleAsync(e))
            {
                var log = scope.Resolve<ILogger>();
                var console = scope.Resolve<IAnsiConsole>();
                log.Error(e, "Exiting due to fatal error");
                console.MarkupLine($"[red]Fatal error:[/] {Markup.Escape(e.Message)}");
            }

            return (int)ExitStatus.Failed;
        }
    }
}
