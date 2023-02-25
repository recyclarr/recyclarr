using Spectre.Console;

namespace Recyclarr.TrashLib.Config.Listers;

public class ConfigLocalLister : IConfigLister
{
    private readonly IAnsiConsole _console;

    public ConfigLocalLister(IAnsiConsole console)
    {
        _console = console;
    }

    public void List()
    {
        _console.Write("Local listing is not supported yet, but coming soon.");
    }
}
