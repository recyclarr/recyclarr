using Recyclarr.Cli.Console.Settings;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class CustomFormatDataLister(
    ILogger log,
    IAnsiConsole console,
    CustomFormatResourceQuery guide
)
{
    public void List(IListCustomFormatSettings settings)
    {
        if (settings.ScoreSets)
        {
            ListScoreSets(settings.Service);
        }
        else
        {
            ListCustomFormats(settings.Service);
        }
    }

    private void ListScoreSets(SupportedServices serviceType)
    {
        console.WriteLine(
            "\nThe following score sets are available. Use these with the `score_set` property in any "
                + "quality profile defined under the top-level `quality_profiles` list."
        );

        console.WriteLine();

        var customFormats = guide.Get(serviceType);

        var scoreSets = customFormats
            .SelectMany(x => x.TrashScores.Keys)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .Order(StringComparer.InvariantCultureIgnoreCase)
            .ToList();

        log.Debug("Found {Count} score sets for {Service}", scoreSets.Count, serviceType);

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

        var customFormats = guide.Get(serviceType);

        var categories = customFormats
            .Where(x => !string.IsNullOrWhiteSpace(x.TrashId))
            .OrderBy(x => x.Name)
            .ToLookup(x => x.Category)
            .OrderBy(x => x.Key)
            .ToList();

        log.Debug(
            "Found {Count} custom formats in {CategoryCount} categories for {Service}",
            customFormats.Count,
            categories.Count,
            serviceType
        );

        // Indentation matches YAML config structure for direct copy-paste into trash_ids
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

        console.WriteLine(
            "The above Custom Formats are in YAML format and ready to be copied & pasted "
                + "under the `trash_ids:` property."
        );
    }
}
