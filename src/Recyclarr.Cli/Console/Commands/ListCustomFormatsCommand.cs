using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Processors;
using Recyclarr.TrashGuide;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8765

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List custom formats in the guide for a particular service.")]
internal class ListCustomFormatsCommand(
    ILogger log,
    IAnsiConsole console,
    CategorizedCustomFormatProvider provider,
    ProviderProgressHandler providerProgressHandler
) : AsyncCommand<ListCustomFormatsCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : ListCommandSettings
    {
        [CommandArgument(0, "<service_type>")]
        [EnumDescription<SupportedServices>("The service type to obtain information about.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices Service { get; init; }

        [CommandOption("--score-sets")]
        [Description("[DEPRECATED] Use 'list score-sets' instead.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool ScoreSets { get; init; }
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        await providerProgressHandler.InitializeProvidersAsync(ct);

        if (settings.ScoreSets)
        {
            const string deprecationMessage =
                "The '--score-sets' option is deprecated and will be removed in a future version. "
                + "Use 'recyclarr list score-sets' instead. "
                + "See: https://recyclarr.dev/guide/upgrade-guide/v8.0/#score-sets-command";

            console.MarkupLine($"[darkorange bold][[DEPRECATED]][/] {deprecationMessage}");
            log.Warning(deprecationMessage);
            ListScoreSets(settings);
        }
        else
        {
            ListCustomFormats(settings);
        }

        return (int)ExitStatus.Succeeded;
    }

    // TODO: Remove this method when --score-sets is removed. This logic is duplicated in
    // ListScoreSetsCommand.cs for the new 'list score-sets' command.
    private void ListScoreSets(CliSettings settings)
    {
        var customFormats = provider.Get(settings.Service);

        var scoreSets = customFormats
            .SelectMany(x => x.Resource.TrashScores.Keys)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .Order(StringComparer.InvariantCultureIgnoreCase)
            .ToList();

        log.Debug("Found {Count} score sets for {Service}", scoreSets.Count, settings.Service);
        log.Information("Score sets: {@ScoreSets}", scoreSets);

        if (settings.Raw)
        {
            OutputScoreSetsRaw(scoreSets);
        }
        else
        {
            OutputScoreSetsTable(scoreSets);
        }
    }

    private void OutputScoreSetsRaw(IReadOnlyCollection<string> scoreSets)
    {
        foreach (var set in scoreSets)
        {
            console.WriteLine(set);
        }
    }

    private void OutputScoreSetsTable(IReadOnlyCollection<string> scoreSets)
    {
        var table = new Table().AddColumn("Score Set Name");
        var alternatingColors = new[] { "white", "paleturquoise4" };
        var colorIndex = 0;

        foreach (var set in scoreSets)
        {
            var color = alternatingColors[colorIndex];
            table.AddRow($"[{color}]{Markup.Escape(set)}[/]");
            colorIndex = 1 - colorIndex;
        }

        console.WriteLine();
        console.Write(table);
        console.WriteLine();
        console.WriteLine(
            "Use these with the `score_set` property in any quality profile defined under "
                + "the top-level `quality_profiles` list."
        );
    }

    private void ListCustomFormats(CliSettings settings)
    {
        var customFormats = provider.Get(settings.Service);

        var items = customFormats
            .Where(x => !string.IsNullOrWhiteSpace(x.Resource.TrashId))
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Resource.Name)
            .ToList();

        log.Debug("Found {Count} custom formats for {Service}", items.Count, settings.Service);
        log.Information(
            "Custom formats: {@CustomFormats}",
            items.Select(cf => cf.Resource.TrashId)
        );

        if (settings.Raw)
        {
            OutputCustomFormatsRaw(items);
        }
        else
        {
            OutputCustomFormatsTable(items);
        }
    }

    private void OutputCustomFormatsRaw(IReadOnlyCollection<CategorizedCustomFormat> items)
    {
        foreach (var cf in items)
        {
            var category = cf.Category ?? "";
            console.WriteLine($"{cf.Resource.TrashId}\t{cf.Resource.Name}\t{category}");
        }
    }

    private void OutputCustomFormatsTable(IReadOnlyCollection<CategorizedCustomFormat> items)
    {
        var byCategory = items.GroupBy(cf => cf.Category ?? "[No Category]").OrderBy(g => g.Key);

        console.WriteLine();

        var maxNameLen = items.Max(cf => cf.Resource.Name.Length);

        foreach (var group in byCategory)
        {
            var table = new Table()
                .AddColumn(new TableColumn("Name").Width(maxNameLen))
                .AddColumn("Trash ID")
                .HideHeaders()
                .Border(TableBorder.None);

            var rowIndex = 0;
            foreach (var cf in group)
            {
                var color = rowIndex++ % 2 == 0 ? "white" : "grey";
                table.AddRow(
                    $"[{color}]{cf.Resource.Name.EscapeMarkup()}[/]",
                    $"[{color}]{cf.Resource.TrashId}[/]"
                );
            }

            var panel = new Panel(table)
                .Header($"[orange3]{group.Key.EscapeMarkup()}[/]")
                .BorderColor(Color.Grey);

            console.Write(panel);
        }

        console.WriteLine();
        console.WriteLine(
            "Copy the Trash ID values to use with the `trash_ids:` property in your config."
        );
    }
}
