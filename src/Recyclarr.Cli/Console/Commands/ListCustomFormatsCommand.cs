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
    internal class CliSettings : BaseCommandSettings
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
            ListScoreSets(settings.Service);
        }
        else
        {
            ListCustomFormats(settings.Service);
        }

        return (int)ExitStatus.Succeeded;
    }

    // TODO: Remove this method when --score-sets is removed. This logic is duplicated in
    // ListScoreSetsCommand.cs for the new 'list score-sets' command.
    private void ListScoreSets(SupportedServices serviceType)
    {
        console.WriteLine(
            "\nThe following score sets are available. Use these with the `score_set` property in any "
                + "quality profile defined under the top-level `quality_profiles` list."
        );

        console.WriteLine();

        var customFormats = provider.Get(serviceType);

        var scoreSets = customFormats
            .SelectMany(x => x.Resource.TrashScores.Keys)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .Order(StringComparer.InvariantCultureIgnoreCase)
            .ToList();

        log.Debug("Found {Count} score sets for {Service}", scoreSets.Count, serviceType);
        log.Information("Score sets: {@ScoreSets}", scoreSets);

        foreach (var set in scoreSets)
        {
            console.WriteLine($"  - {set}");
        }
    }

    private void ListCustomFormats(SupportedServices serviceType)
    {
        console.WriteLine();
        console.WriteLine("List of Custom Formats in the TRaSH Guides:");
        console.WriteLine();

        var customFormats = provider.Get(serviceType);

        var categories = customFormats
            .Where(x => !string.IsNullOrWhiteSpace(x.Resource.TrashId))
            .OrderBy(x => x.Resource.Name)
            .ToLookup(x => x.Category)
            .OrderBy(x => x.Key)
            .ToList();

        log.Debug(
            "Found {Count} custom formats in {CategoryCount} categories for {Service}",
            customFormats.Count,
            categories.Count,
            serviceType
        );
        log.Information(
            "Custom formats: {@CustomFormats}",
            customFormats.Select(cf => cf.Resource.TrashId)
        );

        // Indentation matches YAML config structure for direct copy-paste into trash_ids
        foreach (var cat in categories)
        {
            var title = cat.Key is not null ? $"{cat.Key}" : "[No Category]";

            console.WriteLine($"          # {title}");

            foreach (var cf in cat)
            {
                console.WriteLine($"          - {cf.Resource.TrashId} # {cf.Resource.Name}");
            }

            console.WriteLine();
        }

        console.WriteLine(
            "The above Custom Formats are in YAML format and ready to be copied & pasted "
                + "under the `trash_ids:` property."
        );
    }
}
