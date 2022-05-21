using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using Recyclarr.Command.Initialization;

namespace Recyclarr.Command;

public abstract class ServiceCommand : ICommand, IServiceCommand
{
    private readonly IServiceInitializationAndCleanup _init;

    [CommandOption("preview", 'p', Description =
        "Only display the processed markdown results without making any API calls.")]
    public bool Preview { get; [UsedImplicitly] set; } = false;

    [CommandOption("debug", 'd', Description =
        "Display additional logs useful for development/debug purposes.")]
    public bool Debug { get; [UsedImplicitly] set; } = false;

    [CommandOption("config", 'c', Description =
        "One or more YAML config files to use. All configs will be used and settings are additive. " +
        "If not specified, the script will look for `recyclarr.yml` in the same directory as the executable.")]
    public ICollection<string> Config { get; [UsedImplicitly] set; } =
        new List<string> {AppPaths.DefaultConfigPath};

    public abstract string CacheStoragePath { get; }
    public abstract string Name { get; }

    protected ServiceCommand(IServiceInitializationAndCleanup init)
    {
        _init = init;
    }

    public async ValueTask ExecuteAsync(IConsole console)
        => await _init.Execute(this, Process);

    protected abstract Task Process();
}
