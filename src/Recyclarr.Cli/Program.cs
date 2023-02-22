using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Autofac;
using MoreLinq;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Setup;
using Serilog.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli;

internal static partial class Program
{
    private static ILifetimeScope? _scope;
    private static IBaseCommandSetupTask[] _tasks = Array.Empty<IBaseCommandSetupTask>();
    private static ILogger? _log;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public static int Main(string[] args)
    {
        var builder = new ContainerBuilder();
        CompositionRoot.Setup(builder);

        var logLevelSwitch = new LoggingLevelSwitch();
        builder.RegisterInstance(logLevelSwitch);

        var appDataPathProvider = new AppDataPathProvider();
        builder.RegisterInstance(appDataPathProvider);

        var app = new CommandApp(new AutofacTypeRegistrar(builder, s => _scope = s));
        app.Configure(config =>
        {
        #if DEBUG
            config.PropagateExceptions();
            config.ValidateExamples();
        #endif

            config.Settings.PropagateExceptions = true;
            config.Settings.StrictParsing = true;

            config.SetApplicationName("recyclarr");
            // config.SetApplicationVersion("v1.2.3");

            var interceptor = new CliInterceptor(logLevelSwitch, appDataPathProvider);
            interceptor.OnIntercepted.Subscribe(_ => OnAppInitialized());
            config.SetInterceptor(interceptor);

            CliSetup.Commands(config);
        });

        var result = 1;
        try
        {
            result = app.Run(args);
        }
        catch (CommandRuntimeException ex)
        {
            var msg = CommandMessageRegex().Replace(ex.Message, "[gold1]$0[/]");
            AnsiConsole.Markup($"[red]Error:[/] [white]{msg}[/]");
            _log?.Debug(ex, "Command Exception");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            _log?.Debug(ex, "Non-recoverable Exception");
        }
        finally
        {
            OnAppCleanup();
        }

        return result;
    }

    private static void OnAppInitialized()
    {
        if (_scope is null)
        {
            throw new InvalidProgramException("Composition root is not initialized");
        }

        _log = _scope.Resolve<ILogger>();
        _log.Debug("Recyclarr Version: {Version}", GitVersionInformation.InformationalVersion);

        _tasks = _scope.Resolve<IOrderedEnumerable<IBaseCommandSetupTask>>().ToArray();
        _tasks.ForEach(x => x.OnStart());
    }

    private static void OnAppCleanup()
    {
        _tasks.Reverse().ForEach(x => x.OnFinish());
    }

    [GeneratedRegex("'.*?'")]
    private static partial Regex CommandMessageRegex();
}
