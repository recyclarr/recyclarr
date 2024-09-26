using Autofac;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Console.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = new ContainerBuilder();
        CompositionRoot.Setup(builder);

        var app = new CommandApp(new AutofacTypeRegistrar(builder));
        app.Configure(config =>
        {
        #if DEBUG
            config.ValidateExamples();
        #endif

            config.Settings.StrictParsing = true;

            config.SetApplicationName("recyclarr");
            config.SetApplicationVersion(
                $"v{GitVersionInformation.SemVer} ({GitVersionInformation.FullBuildMetaData})");

            config.SetExceptionHandler(ExceptionHandler);

            CliSetup.Commands(config);
        });

        return await app.RunAsync(args);
    }

    private static void ExceptionHandler(Exception ex, ITypeResolver? resolver = null)
    {
        var log = (ILogger?) resolver?.Resolve(typeof(ILogger));
        if (log is null)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
        else
        {
            log.Error(ex, "Non-recoverable Exception");
        }
    }
}
