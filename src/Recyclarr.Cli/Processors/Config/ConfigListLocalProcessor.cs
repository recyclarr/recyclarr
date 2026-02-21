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
    ILogger log,
    IAnsiConsole console,
    ConfigurationRegistry configRegistry,
    IAppPaths paths
)
{
    public void Process()
    {
        var result = configRegistry.FindAndLoadConfigs();
        ConfigFailureRenderer.Render(console, log, result);

        var allConfigs = result.Configs.ToList();
        var configsByFile = allConfigs.ToLookup(x => MakeRelative(x.YamlPath));

        var radarrCount = allConfigs.Count(x => x.ServiceType == SupportedServices.Radarr);
        var sonarrCount = allConfigs.Count(x => x.ServiceType == SupportedServices.Sonarr);

        log.Information(
            "Found {FileCount} config files with {RadarrCount} Radarr and {SonarrCount} Sonarr instances",
            configsByFile.Count,
            radarrCount,
            sonarrCount
        );

        log.Debug(
            "Local configs: {@Configs}",
            allConfigs.Select(x => new { x.InstanceName, Service = x.ServiceType.ToString() })
        );

        console.WriteLine();
        console.WriteLine("Local configuration files:");
        console.WriteLine();

        var tree = new Tree(paths.ConfigDirectory.ToString()!);

        foreach (var pair in configsByFile)
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

        console.Write(tree);
    }

    private string MakeRelative(IFileInfo? path)
    {
        if (path is null)
        {
            return "<no path>";
        }

        var configPath = new Uri(path.FullName, UriKind.Absolute);
        var configDir = new Uri(paths.YamlConfigDirectory.FullName, UriKind.Absolute);
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
