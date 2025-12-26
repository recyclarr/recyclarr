using System.Globalization;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.ConfigTemplates;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Config;

internal class ConfigListTemplateProcessor(
    ILogger log,
    IAnsiConsole console,
    ConfigTemplatesResourceQuery templatesService,
    ConfigIncludesResourceQuery includesService
)
{
    public void Process(IConfigListTemplatesSettings settings)
    {
        if (settings.Includes)
        {
            ListIncludes();
            return;
        }

        ListTemplates();
    }

    private void ListIncludes()
    {
        var sonarrIds = includesService
            .Get(SupportedServices.Sonarr)
            .Where(x => !x.Hidden)
            .Select(x => x.Id)
            .ToList();

        var radarrIds = includesService
            .Get(SupportedServices.Radarr)
            .Where(x => !x.Hidden)
            .Select(x => x.Id)
            .ToList();

        log.Information(
            "Found {SonarrCount} Sonarr and {RadarrCount} Radarr includes",
            sonarrIds.Count,
            radarrIds.Count
        );

        log.Debug("Sonarr includes: {@Includes}", sonarrIds);
        log.Debug("Radarr includes: {@Includes}", radarrIds);

        console.WriteLine();
        console.WriteLine("Reusable include templates:");
        console.WriteLine();

        WriteTable(sonarrIds, radarrIds);

        console.WriteLine();
        console.WriteLine("Use these with the `include` property in your config files.");
    }

    private void ListTemplates()
    {
        var sonarrIds = templatesService
            .Get(SupportedServices.Sonarr)
            .Where(x => !x.Hidden)
            .Select(x => x.Id)
            .ToList();

        var radarrIds = templatesService
            .Get(SupportedServices.Radarr)
            .Where(x => !x.Hidden)
            .Select(x => x.Id)
            .ToList();

        log.Information(
            "Found {SonarrCount} Sonarr and {RadarrCount} Radarr templates",
            sonarrIds.Count,
            radarrIds.Count
        );

        log.Debug("Sonarr templates: {@Templates}", sonarrIds);
        log.Debug("Radarr templates: {@Templates}", radarrIds);

        console.WriteLine();
        console.WriteLine("Configuration templates:");
        console.WriteLine();

        WriteTable(sonarrIds, radarrIds);

        console.WriteLine();
        console.WriteLine("Use these with `recyclarr config create -t <template>`.");
    }

    private void WriteTable(IReadOnlyList<string> sonarrIds, IReadOnlyList<string> radarrIds)
    {
        var table = new Table();
        var empty = new Markup("");

        var sonarrMarkup = sonarrIds
            .Select(id => Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[blue]{id}[/]"))
            .ToList();

        var radarrMarkup = radarrIds
            .Select(id => Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[blue]{id}[/]"))
            .ToList();

        table.AddColumn(SupportedServices.Sonarr.ToString());
        table.AddColumn(SupportedServices.Radarr.ToString());

        var items = sonarrMarkup.ZipLongest(radarrMarkup, (s, r) => (s ?? empty, r ?? empty));

        foreach (var (s, r) in items)
        {
            table.AddRow(s, r);
        }

        console.Write(table);
    }
}
