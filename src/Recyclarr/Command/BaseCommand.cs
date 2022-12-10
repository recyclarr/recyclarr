using Autofac;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Flurl.Http;
using JetBrains.Annotations;
using MoreLinq.Extensions;
using Recyclarr.Command.Setup;
using Recyclarr.Logging;
using Serilog;
using Serilog.Events;
using TrashLib.Http;
using TrashLib.Repo.VersionControl;
using TrashLib.Startup;

namespace Recyclarr.Command;

public abstract class BaseCommand : ICommand
{
    // Not explicitly defined as a Command here because for legacy reasons, different subcommands expose this in
    // different ways to the user.
    public abstract string? AppDataDirectory { get; set; }

    [CommandOption("debug", 'd', Description =
        "Display additional logs useful for development/debug purposes.")]
    // ReSharper disable once MemberCanBeProtected.Global
    public bool Debug { get; [UsedImplicitly] set; } = false;

    protected ILogger Logger { get; private set; } = Log.Logger;

    protected virtual void RegisterServices(ContainerBuilder builder)
    {
    }

    public virtual async ValueTask ExecuteAsync(IConsole console)
    {
        // Must happen first because everything can use the logger.
        var logLevel = Debug ? LogEventLevel.Debug : LogEventLevel.Information;

        await using var container = CompositionRoot.Setup(builder =>
        {
            builder.RegisterInstance(console).As<IConsole>().ExternallyOwned();

            builder.Register(c => c.Resolve<DefaultAppDataSetup>().CreateAppPaths(AppDataDirectory))
                .As<IAppPaths>()
                .SingleInstance();

            builder.Register(c => c.Resolve<LoggerFactory>().Create(logLevel))
                .As<ILogger>()
                .SingleInstance();

            RegisterServices(builder);
        });

        Logger = container.Resolve<ILogger>();
        var tasks = container.Resolve<IOrderedEnumerable<IBaseCommandSetupTask>>().ToArray();
        tasks.ForEach(x => x.OnStart());

        try
        {
            await Process(container);
        }
        catch (Exception e)
        {
            switch (e)
            {
                case GitCmdException e2:
                    Logger.Error(e2, "Non-zero exit code {ExitCode} while executing Git command: {Error}",
                        e2.ExitCode, e2.Error);
                    break;

                case FlurlHttpException e2:
                    Logger.Error("HTTP error: {Message}", e2.SanitizedExceptionMessage());
                    break;

                default:
                    Logger.Error(e, "Non-recoverable exception");
                    break;
            }

            throw new CommandException("Exiting due to exception");
        }
        finally
        {
            tasks.Reverse().ForEach(x => x.OnFinish());
        }
    }

    public abstract Task Process(ILifetimeScope container);
}
