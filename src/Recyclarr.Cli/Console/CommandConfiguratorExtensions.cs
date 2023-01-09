using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console;

public static class CommandConfiguratorExtensions
{
    public static ICommandConfigurator WithExample(this ICommandConfigurator cli, params string[] args)
    {
        return cli.WithExample(args);
    }
}
