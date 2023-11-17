using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Recyclarr.Platform;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Processors.Config;

public class ConfigListLocalProcessor(
    IAnsiConsole console,
    IConfigurationFinder configFinder,
    IConfigurationLoader configLoader,
    IAppPaths paths)
{
    public void Process()
    {
        var tree = new Tree(paths.AppDataDirectory.ToString()!);

        foreach (var configPath in configFinder.GetConfigFiles())
        {
            var configs = configLoader.Load(configPath);

            var rows = new List<IRenderable>();
            BuildInstanceTree(rows, configs, SupportedServices.Radarr);
            BuildInstanceTree(rows, configs, SupportedServices.Sonarr);

            if (rows.Count == 0)
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

        console.WriteLine();
        console.Write(tree);
    }

    private string MakeRelative(IFileInfo path)
    {
        var configPath = new Uri(path.FullName, UriKind.Absolute);
        var configDir = new Uri(paths.ConfigsDirectory.FullName, UriKind.Absolute);
        return configDir.MakeRelativeUri(configPath).ToString();
    }

    private static void BuildInstanceTree(
        List<IRenderable> rows,
        IReadOnlyCollection<IServiceConfiguration> registry,
        SupportedServices service)
    {
        var configs = registry.GetConfigsOfType(service).ToList();
        if (configs.Count == 0)
        {
            return;
        }

        var tree = new Tree(Markup.FromInterpolated($"[red]{service}[/]"));
        tree.AddNodes(configs.Select(c =>
            Markup.FromInterpolated($"[blue]{c.InstanceName}[/]")));

        rows.Add(tree);
    }
}
