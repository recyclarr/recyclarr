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

        console.WriteLine();
        console.Write(new Rule("[bold red]Configuration Errors[/]").LeftJustified());
        console.WriteLine();

        var renderables = context.Results.Select(x => new Padder(x.Render()).PadBottom(2));

        var main = new Panel(new Rows(renderables)) { Width = 40 }
            .Header("[red]Configuration Errors[/]")
            .Padding(2, 2)
            .RoundedBorder();

        console.Write(main);

        return filteredConfigs;
    }
}
