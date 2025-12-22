using System.Globalization;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.ConfigTemplates;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Config;

internal class ConfigListTemplateProcessor(
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
        var table = new Table();
        var empty = new Markup("");

        var sonarrIncludes = includesService
            .Get(SupportedServices.Sonarr)
            .Where(x => !x.Hidden)
            .Select(x => Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[blue]{x.Id}[/]"))
            .ToList();

        var radarrIncludes = includesService
            .Get(SupportedServices.Radarr)
            .Where(x => !x.Hidden)
            .Select(x => Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[blue]{x.Id}[/]"))
            .ToList();

        table.AddColumn(SupportedServices.Sonarr.ToString());
        table.AddColumn(SupportedServices.Radarr.ToString());

        var items = sonarrIncludes.ZipLongest(radarrIncludes, (s, r) => (s ?? empty, r ?? empty));

        foreach (var (s, r) in items)
        {
            table.AddRow(s, r);
        }

        console.Write(table);
    }

    private void ListTemplates()
    {
        var table = new Table();
        var empty = new Markup("");

        var sonarrTemplates = templatesService
            .Get(SupportedServices.Sonarr)
            .Where(x => !x.Hidden)
            .Select(x => Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[blue]{x.Id}[/]"))
            .ToList();

        var radarrTemplates = templatesService
            .Get(SupportedServices.Radarr)
            .Where(x => !x.Hidden)
            .Select(x => Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[blue]{x.Id}[/]"))
            .ToList();

        table.AddColumn(SupportedServices.Sonarr.ToString());
        table.AddColumn(SupportedServices.Radarr.ToString());

        var items = sonarrTemplates.ZipLongest(radarrTemplates, (s, r) => (s ?? empty, r ?? empty));

        foreach (var (s, r) in items)
        {
            table.AddRow(s, r);
        }

        console.Write(table);
    }
}
