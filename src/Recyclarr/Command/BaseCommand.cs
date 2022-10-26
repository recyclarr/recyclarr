using Autofac;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using MoreLinq.Extensions;
using Recyclarr.Command.Setup;
using Recyclarr.Logging;
using Serilog;
using Serilog.Events;
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

        var tasks = container.Resolve<IOrderedEnumerable<IBaseCommandSetupTask>>().ToArray();
        tasks.ForEach(x => x.OnStart());

        try
        {
            await Process(container);
        }
        finally
        {
            tasks.Reverse().ForEach(x => x.OnFinish());
        }
    }

    public abstract Task Process(ILifetimeScope container);
}
