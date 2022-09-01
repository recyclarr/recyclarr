using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using MoreLinq.Extensions;
using Recyclarr.Command.Setup;
using Serilog.Events;

namespace Recyclarr.Command;

public abstract class BaseCommand : ICommand
{
    // Not explicitly defined as a Command here because for legacy reasons, different subcommands expose this in
    // different ways to the user.
    public abstract string? AppDataDirectory { get; set; }

    [CommandOption("debug", 'd', Description =
        "Display additional logs useful for development/debug purposes.")]
    public bool Debug { get; [UsedImplicitly] set; } = false;

    public static ICompositionRoot? CompositionRoot { get; set; }

    public virtual async ValueTask ExecuteAsync(IConsole console)
    {
        // Must happen first because everything can use the logger.
        var logLevel = Debug ? LogEventLevel.Debug : LogEventLevel.Information;

        if (CompositionRoot is null)
        {
            throw new CommandException("CompositionRoot must not be null");
        }

        using var container = CompositionRoot.Setup(AppDataDirectory, console, logLevel);

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

    public abstract Task Process(IServiceLocatorProxy container);
}
