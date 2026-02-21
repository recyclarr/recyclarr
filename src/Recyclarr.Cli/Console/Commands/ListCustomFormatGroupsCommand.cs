using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Processors;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console;
using Spectre.Console.Cli;

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
    }

    public override async Task<int> ExecuteAsync(
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
            OutputTable(groups);
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

    private void OutputTable(IReadOnlyCollection<CfGroupResource> groups)
    {
        console.WriteLine();

        foreach (var group in groups)
        {
            var table = new Table()
                .AddColumns("Name", "Trash ID", "Required", "Default")
                .Border(TableBorder.Simple);

            var rowIndex = 0;
            foreach (var cf in group.CustomFormats.OrderBy(c => c.Name))
            {
                var color = rowIndex++ % 2 == 0 ? "white" : "grey";
                table.AddRow(
                    $"[{color}]{cf.Name.EscapeMarkup()}[/]",
                    $"[{color}]{cf.TrashId}[/]",
                    $"[{color}]{(cf.Required ? "Yes" : "No")}[/]",
                    $"[{color}]{(cf.Default ? "Yes" : "No")}[/]"
                );
            }

            var content = new Rows(table);

            var profileNames = group.QualityProfiles.Include.Keys.Order().ToList();
            if (profileNames.Count > 0)
            {
                var profileLines = profileNames.Select(p => new Markup($"  {p.EscapeMarkup()}"));
                content = new Rows([
                    table,
                    new Markup("[blue]Quality Profiles:[/]"),
                    .. profileLines,
                ]);
            }

            var header =
                $"[orange3]{group.Name.EscapeMarkup()}[/]" + $"  [grey]({group.TrashId})[/]";

            var panel = new Panel(content).Header(new PanelHeader(header)).BorderColor(Color.Grey);

            console.Write(panel);
        }

        console.WriteLine();
        console.WriteLine(
            "Copy the Trash ID values to use with the `custom_format_groups:` property in your config."
        );
    }
}
