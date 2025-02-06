using Recyclarr.Config.Parsing;
using Spectre.Console;

namespace Recyclarr.Config.Filtering;

public class ConfigFilterProcessor(IAnsiConsole console, IEnumerable<IConfigFilter> filters)
{
    public IReadOnlyCollection<LoadedConfigYaml> FilterAndRender(
        ConfigFilterCriteria criteria,
        IReadOnlyCollection<LoadedConfigYaml> configs
    )
    {
        var context = new FilterContext();

        var filteredConfigs = filters.Aggregate(
            configs,
            (current, filter) => filter.Filter(criteria, current, context)
        );

        var renderables = context
            .Results.Select(x => new Padder(x.Render()).Padding(0, 0, 0, 1))
            .ToList();

        if (renderables.Count != 0)
        {
            var main = new Panel(new Padder(new Rows(renderables).Collapse()).PadBottom(0))
                .Collapse()
                .Header("[red]Configuration Errors[/]")
                .RoundedBorder();

            var column = new Columns(main);

            console.Write(column);
        }

        return filteredConfigs;
    }
}
