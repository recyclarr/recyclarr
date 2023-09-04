using Recyclarr.Cli.Console.Commands;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Config;

public class ConfigListTemplateProcessor
{
    private readonly IAnsiConsole _console;
    private readonly IConfigTemplateGuideService _guideService;

    public ConfigListTemplateProcessor(IAnsiConsole console, IConfigTemplateGuideService guideService)
    {
        _console = console;
        _guideService = guideService;
    }

    public void Process(IConfigListTemplatesSettings settings)
    {
        if (settings.Includes)
        {
            ListData(_guideService.GetIncludeData());
            return;
        }

        ListData(_guideService.GetTemplateData());
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
