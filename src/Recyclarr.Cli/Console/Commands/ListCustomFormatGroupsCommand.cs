using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        await providerProgressHandler.InitializeProvidersAsync(settings.Raw, ct);

        var groups = cfGroupQuery.Get(settings.Service).OrderBy(g => g.Name).ToList();

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
            var cfCount = group.CustomFormats.Count.ToString(CultureInfo.InvariantCulture);
            console.WriteLine($"{group.TrashId}\t{group.Name}\t{cfCount}");
        }
    }

    private void OutputTable(IReadOnlyCollection<CfGroupResource> groups)
    {
        var table = new Table().AddColumns("Name", "Trash ID", "CF Count");
        var alternatingColors = new[] { "white", "paleturquoise4" };
        var colorIndex = 0;

        foreach (var group in groups)
        {
            var color = alternatingColors[colorIndex];
            var cfCount = group.CustomFormats.Count.ToString(CultureInfo.InvariantCulture);
            table.AddRow(
                $"[{color}]{Markup.Escape(group.Name)}[/]",
                $"[{color}]{Markup.Escape(group.TrashId)}[/]",
                $"[{color}]{cfCount}[/]"
            );
            colorIndex = 1 - colorIndex;
        }

        console.WriteLine();
        console.MarkupLine("[orange3]Custom Format Groups in the TRaSH Guides[/]");
        console.WriteLine();
        console.Write(table);
        console.WriteLine();
        console.WriteLine(
            "Copy the Trash ID values to use with the `custom_format_groups:` property in your config."
        );
    }
}
