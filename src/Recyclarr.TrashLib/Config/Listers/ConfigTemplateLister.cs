using Recyclarr.TrashLib.Config.Services;
using Spectre.Console;

namespace Recyclarr.TrashLib.Config.Listers;

public class ConfigTemplateLister : IConfigLister
{
    private readonly IAnsiConsole _console;
    private readonly IConfigTemplateGuideService _guideService;

    public ConfigTemplateLister(
        IAnsiConsole console,
        IConfigTemplateGuideService guideService)
    {
        _console = console;
        _guideService = guideService;
    }

    public async Task List()
    {
        var data = await _guideService.LoadTemplateData();

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

        _console.Write(table);
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
