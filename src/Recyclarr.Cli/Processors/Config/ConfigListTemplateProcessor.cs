using System.Globalization;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.ConfigTemplates;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Config;

internal class ConfigListTemplateProcessor(
    IAnsiConsole console,
    IConfigTemplatesResourceQuery guideService
)
{
    public void Process(IConfigListTemplatesSettings settings)
    {
        if (settings.Includes)
        {
            ListIncludes(guideService.GetIncludes());
            return;
        }

        ListTemplates(guideService.GetTemplates());
    }

    private void ListTemplates(IReadOnlyCollection<TemplatePath> data)
    {
        var table = new Table();
        var empty = new Markup("");

        var sonarrRowItems = RenderTemplates(table, data, SupportedServices.Sonarr);
        var radarrRowItems = RenderTemplates(table, data, SupportedServices.Radarr);
        var items = sonarrRowItems.ZipLongest(radarrRowItems, (s, r) => (s ?? empty, r ?? empty));

        foreach (var (s, r) in items)
        {
            table.AddRow(s, r);
        }

        console.Write(table);
    }

    private void ListIncludes(IReadOnlyCollection<IncludePath> includes)
    {
        var table = new Table();
        var empty = new Markup("");

        var sonarrRowItems = RenderIncludes(table, includes, SupportedServices.Sonarr);
        var radarrRowItems = RenderIncludes(table, includes, SupportedServices.Radarr);
        var items = sonarrRowItems.ZipLongest(radarrRowItems, (s, r) => (s ?? empty, r ?? empty));

        foreach (var (s, r) in items)
        {
            table.AddRow(s, r);
        }

        console.Write(table);
    }

    private static List<Markup> RenderIncludes(
        Table table,
        IEnumerable<IncludePath> includePaths,
        SupportedServices service
    )
    {
        var paths = includePaths
            .Where(x => x.Service == service)
            .Select(x => Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[blue]{x.Id}[/]"))
            .ToList();

        table.AddColumn(service.ToString());

        return paths;
    }

    private static List<Markup> RenderTemplates(
        Table table,
        IEnumerable<TemplatePath> templatePaths,
        SupportedServices service
    )
    {
        var paths = templatePaths
            .Where(x => x.Service == service && !x.Hidden)
            .Select(x => Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[blue]{x.Id}[/]"))
            .ToList();

        table.AddColumn(service.ToString());

        return paths;
    }
}
