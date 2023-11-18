using Recyclarr.Cli.Console.Commands;
using Recyclarr.Common;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Config;

public class ConfigListTemplateProcessor(IAnsiConsole console, IConfigTemplateGuideService guideService)
{
    public void Process(IConfigListTemplatesSettings settings)
    {
        if (settings.Includes)
        {
            ListData(guideService.GetIncludeData());
            return;
        }

        ListData(guideService.GetTemplateData());
    }

    private void ListData(IReadOnlyCollection<TemplatePath> data)
    {
        var table = new Table();
        var empty = new Markup("");

        var sonarrRowItems = RenderTemplates(table, data, SupportedServices.Sonarr);
        var radarrRowItems = RenderTemplates(table, data, SupportedServices.Radarr);
        var items = sonarrRowItems
            .ZipLongest(radarrRowItems, (s, r) => (s ?? empty, r ?? empty));

        foreach (var (s, r) in items)
        {
            table.AddRow(s, r);
        }

        console.Write(table);
    }

    private static IEnumerable<Markup> RenderTemplates(
        Table table,
        IEnumerable<TemplatePath> templatePaths,
        SupportedServices service)
    {
        var paths = templatePaths
            .Where(x => x.Service == service && !x.Hidden)
            .Select(x => Markup.FromInterpolated($"[blue]{x.Id}[/]"))
            .ToList();

        table.AddColumn(service.ToString());

        return paths;
    }
}
