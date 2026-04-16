using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Processors;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List custom format groups available in the guide.")]
internal class ListCustomFormatGroupsCommand(
    ILogger log,
    IAnsiConsole console,
    CfGroupResourceQuery cfGroupQuery,
    ProviderProgressHandler providerProgressHandler
) : AsyncCommand<ListCustomFormatGroupsCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : ListCommandSettings
    {
        [CommandArgument(0, "<service_type>")]
        [EnumDescription<SupportedServices>("The service type to obtain information about.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices Service { get; init; }

        [CommandOption("--filter")]
        [Description("Filter groups by name (case-insensitive substring match)")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string? Filter { get; init; }

        [CommandOption("--details")]
        [Description("Show member custom formats and their required/default status")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Details { get; init; }
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        await providerProgressHandler.InitializeProvidersAsync(settings.Raw, ct);

        var groups = cfGroupQuery.Get(settings.Service).OrderBy(g => g.Name).ToList();

        if (settings.Filter is not null)
        {
            groups = groups
                .Where(g => g.Name.Contains(settings.Filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        log.Debug(
            "Found {Count} custom format groups for {Service}",
            groups.Count,
            settings.Service
        );
        log.Information("Custom format groups: {@Groups}", groups.Select(g => g.TrashId));

        if (settings.Raw)
        {
            OutputRaw(groups);
        }
        else
        {
            OutputTable(groups, settings.Details);
        }

        return (int)ExitStatus.Succeeded;
    }

    private void OutputRaw(IReadOnlyCollection<CfGroupResource> groups)
    {
        foreach (var group in groups)
        {
            foreach (var cf in group.CustomFormats)
            {
                var required = cf.Required.ToString().ToLowerInvariant();
                var isDefault = cf.Default.ToString().ToLowerInvariant();
                console.WriteLine(
                    $"{group.TrashId}\t{group.Name}\t{cf.TrashId}\t{cf.Name}\t{required}\t{isDefault}"
                );
            }
        }
    }

    private void OutputTable(IReadOnlyCollection<CfGroupResource> groups, bool showDetails)
    {
        console.WriteLine();

        if (showDetails)
        {
            OutputDetailedPanels(groups);
        }
        else
        {
            OutputCompactTable(groups);
        }

        console.WriteLine();
        console.WriteLine(
            "Copy the Trash ID values to use with the `custom_format_groups:` property in your config."
        );
    }

    private void OutputCompactTable(IReadOnlyCollection<CfGroupResource> groups)
    {
        var table = new Table().RoundedBorder();
        table.AddColumn("Group");
        table.AddColumn("Trash ID");

        foreach (var group in groups)
        {
            table.AddRow(Markup.Escape(group.Name), $"[dim]{group.TrashId}[/]");
        }

        console.Write(table);
    }

    private void OutputDetailedPanels(IReadOnlyCollection<CfGroupResource> groups)
    {
        foreach (var group in groups)
        {
            var rows = new List<IRenderable>
            {
                new Markup($"[bold orange3]{group.Name.EscapeMarkup()}[/]"),
                new Markup($"[dim]{group.TrashId}[/]"),
                new Markup("\n[underline]Custom Formats[/]"),
            };

            foreach (var cf in group.CustomFormats.OrderBy(c => c.Name))
            {
                rows.Add(FormatCustomFormatRow(cf));
            }

            var profileNames = group.QualityProfiles.Include.Keys.Order().ToList();
            if (profileNames.Count > 0)
            {
                var groupDefault = string.Equals(
                    group.Default,
                    "true",
                    StringComparison.OrdinalIgnoreCase
                );

                var tag = groupDefault ? "[green](default)[/]" : "[yellow](optional)[/]";

                rows.Add(new Markup("\n[underline]Quality Profiles[/]"));
                rows.AddRange(profileNames.Select(p => new Markup($"  {p.EscapeMarkup()} {tag}")));
            }

            var panel = new Panel(new Rows(rows)).BorderColor(Color.Grey);

            console.Write(panel);
        }
    }

    private static Grid FormatCustomFormatRow(CfGroupCustomFormat cf)
    {
        var tag =
            cf.Required ? "[red]required[/]"
            : cf.Default ? "[green]default[/]"
            : "[yellow]optional[/]";

        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(2).NoWrap().PadLeft(2).PadRight(1));
        grid.AddColumn(new GridColumn().PadLeft(0));
        grid.AddRow(new Markup("[dim]•[/]"), new Markup($"{cf.Name.EscapeMarkup()} {tag}"));

        return grid;
    }
}
