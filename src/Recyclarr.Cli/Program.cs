using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.ErrorHandling;
using Spectre.Console.Cli;

namespace Recyclarr.Cli;

internal static class Program
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification =
        "Top level catch-all to translate exceptions; lack of specificity is intentional")]
    public static async Task<int> Main(string[] args)
    {
        var builder = new ContainerBuilder();
        CompositionRoot.Setup(builder);
        var scope = builder.Build();

        try
        {
            var app = scope.Resolve<CommandApp>();
            app.Configure(config =>
            {
            #if DEBUG
                config.ValidateExamples();
            #endif

                config.PropagateExceptions();
                config.UseStrictParsing();

                config.SetApplicationName("recyclarr");
                config.SetApplicationVersion(
                    $"v{GitVersionInformation.SemVer} ({GitVersionInformation.FullBuildMetaData})");

                CliSetup.Commands(config);
            });

            return await app.RunAsync(args);
        }
        catch (Exception e)
        {
            var log = scope.Resolve<ILogger>();
            var exceptionHandler = new ConsoleExceptionHandler(log);
            await exceptionHandler.HandleException(e);
            return (int) ExitStatus.Failed;
        }
    }
}
