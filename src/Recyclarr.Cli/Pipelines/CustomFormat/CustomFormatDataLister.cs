using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Logging;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class CustomFormatDataLister(IAnsiConsole console, CustomFormatResourceQuery guide)
{
    public void List(IConsoleOutputSettings outputSettings, IListCustomFormatSettings settings)
    {
        switch (settings)
        {
            case { ScoreSets: true }:
                ListScoreSets(settings.Service, outputSettings.IsRawOutputEnabled);
                break;

            default:
                ListCustomFormats(settings.Service, outputSettings.IsRawOutputEnabled);
                break;
        }
    }

    private void ListScoreSets(SupportedServices serviceType, bool raw)
    {
        if (!raw)
        {
            console.WriteLine(
                "\nThe following score sets are available. Use these with the `score_set` property in any "
                    + "quality profile defined under the top-level `quality_profiles` list."
            );

            console.WriteLine();
        }

        var customFormats = guide.Get(serviceType);

        var scoreSets = customFormats
            .SelectMany(x => x.TrashScores.Keys)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .Order(StringComparer.InvariantCultureIgnoreCase);

        foreach (var set in scoreSets)
        {
            console.WriteLine(raw ? set : $" - {set}");
        }
    }

    private void ListCustomFormats(SupportedServices serviceType, bool raw)
    {
        if (!raw)
        {
            console.WriteLine();
            console.WriteLine("List of Custom Formats in the TRaSH Guides:");
            console.WriteLine();
        }

        var customFormats = guide.Get(serviceType);

        var categories = customFormats
            .Where(x => !string.IsNullOrWhiteSpace(x.TrashId))
            .OrderBy(x => x.Name)
            .ToLookup(x => x.Category)
            .OrderBy(x => x.Key);

        foreach (var cat in categories)
        {
            var title = cat.Key is not null ? $"{cat.Key}" : "[No Category]";

            console.WriteLine($"          # {title}");

            foreach (var cf in cat)
            {
                console.WriteLine($"          - {cf.TrashId} # {cf.Name}");
            }

            console.WriteLine();
        }

        if (!raw)
        {
            console.WriteLine(
                "The above Custom Formats are in YAML format and ready to be copied & pasted "
                    + "under the `trash_ids:` property."
            );
        }
    }
}
