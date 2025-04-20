using Recyclarr.Config.Filtering;
using Spectre.Console;

namespace Recyclarr.Cli.ConfigFilterRendering;

internal class ConsoleFilterResultRenderer(
    IAnsiConsole console,
    IReadOnlyCollection<IConsoleFilterResultRenderer> renderers
) : IFilterResultRenderer
{
    private Dictionary<Type, IConsoleFilterResultRenderer> RenderersByType { get; } =
        renderers.ToDictionary(x => x.CompatibleFilterResult);

    public void RenderResults(IReadOnlyCollection<IFilterResult> results)
    {
        var renderables = results.Select(x =>
        {
            var renderer = RenderersByType[x.GetType()];
            return new Padder(renderer.RenderResults(x)).Padding(0, 0, 0, 1);
        });

        var main = new Panel(new Padder(new Rows(renderables).Collapse()).PadBottom(0))
            .Collapse()
            .Header("[red]Configuration Errors[/]")
            .RoundedBorder();

        var column = new Columns(main);

        console.Write(column);
    }
}
