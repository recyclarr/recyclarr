using System.Globalization;
using System.IO.Abstractions;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Platform;
using Recyclarr.TrashGuide;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Processors.Config;

internal class ConfigListLocalProcessor(
    IAnsiConsole console,
    ConfigurationRegistry configRegistry,
    IAppPaths paths
)
{
    public void Process()
    {
        var tree = new Tree(paths.AppDataDirectory.ToString()!);
        var allConfigs = configRegistry
            .FindAndLoadConfigs()
            .ToLookup(x => MakeRelative(x.YamlPath));

        foreach (var pair in allConfigs)
        {
            var path = pair.Key;
            var configs = pair.ToList();

            var rows = new List<IRenderable>();
            BuildInstanceTree(rows, configs, SupportedServices.Radarr);
            BuildInstanceTree(rows, configs, SupportedServices.Sonarr);

            if (rows.Count == 0)
            {
                rows.Add(new Markup("([red]Empty[/])"));
            }

            var configTree = new Tree(
                Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[b]{path}[/]")
            );
            foreach (var r in rows)
            {
                configTree.AddNode(r);
            }

            tree.AddNode(configTree);
        }

        console.WriteLine();
        console.Write(tree);
    }

    private string MakeRelative(IFileInfo? path)
    {
        if (path is null)
        {
            return "<no path>";
        }

        var configPath = new Uri(path.FullName, UriKind.Absolute);
        var configDir = new Uri(paths.ConfigsDirectory.FullName, UriKind.Absolute);
        return configDir.MakeRelativeUri(configPath).ToString();
    }

    private static void BuildInstanceTree(
        List<IRenderable> rows,
        IReadOnlyCollection<IServiceConfiguration> registry,
        SupportedServices service
    )
    {
        var configs = registry.Where(x => x.ServiceType == service).ToList();
        if (configs.Count == 0)
        {
            return;
        }

        var tree = new Tree(
            Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[red]{service}[/]")
        );
        tree.AddNodes(
            configs.Select(c =>
                Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[blue]{c.InstanceName}[/]")
            )
        );

        rows.Add(tree);
    }
}
