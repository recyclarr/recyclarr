using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.Platform;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Processors.Config;

public class ConfigListLocalProcessor
{
    private readonly IAnsiConsole _console;
    private readonly IConfigurationFinder _configFinder;
    private readonly IConfigurationLoader _configLoader;
    private readonly IAppPaths _paths;

    public ConfigListLocalProcessor(
        IAnsiConsole console,
        IConfigurationFinder configFinder,
        IConfigurationLoader configLoader,
        IAppPaths paths)
    {
        _console = console;
        _configFinder = configFinder;
        _configLoader = configLoader;
        _paths = paths;
    }

    public void Process()
    {
        var tree = new Tree(_paths.AppDataDirectory.ToString()!);

        foreach (var configPath in _configFinder.GetConfigFiles())
        {
            var configs = _configLoader.Load(configPath);

            var rows = new List<IRenderable>();
            BuildInstanceTree(rows, configs, SupportedServices.Radarr);
            BuildInstanceTree(rows, configs, SupportedServices.Sonarr);

            if (!rows.Any())
            {
                rows.Add(new Markup("([red]Empty[/])"));
            }

            var configTree = new Tree(Markup.FromInterpolated($"[b]{MakeRelative(configPath)}[/]"));
            foreach (var r in rows)
            {
                configTree.AddNode(r);
            }

            tree.AddNode(configTree);
        }

        _console.WriteLine();
        _console.Write(tree);
    }

    private string MakeRelative(IFileInfo path)
    {
        var configPath = new Uri(path.FullName, UriKind.Absolute);
        var configDir = new Uri(_paths.ConfigsDirectory.FullName, UriKind.Absolute);
        return configDir.MakeRelativeUri(configPath).ToString();
    }

    private static void BuildInstanceTree(
        ICollection<IRenderable> rows,
        IReadOnlyCollection<IServiceConfiguration> registry,
        SupportedServices service)
    {
        var configs = registry.GetConfigsOfType(service).ToList();
        if (!configs.Any())
        {
            return;
        }

        var tree = new Tree(Markup.FromInterpolated($"[red]{service}[/]"));
        tree.AddNodes(configs.Select(c =>
            Markup.FromInterpolated($"[blue]{c.InstanceName}[/]")));

        rows.Add(tree);
    }
}
