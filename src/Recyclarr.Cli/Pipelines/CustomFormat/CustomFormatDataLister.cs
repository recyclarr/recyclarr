using Recyclarr.Cli.Console.Settings;
using Recyclarr.Common;
using Recyclarr.TrashGuide.CustomFormat;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

public class CustomFormatDataLister(IAnsiConsole console, ICustomFormatGuideService guide)
{
    public void List(IListCustomFormatSettings settings)
    {
        switch (settings)
        {
            case {ScoreSets: true}:
                ListScoreSets(settings.Service, settings.Raw);
                break;

            default:
                ListCustomFormats(settings.Service, settings.Raw);
                break;
        }
    }

    private void ListScoreSets(SupportedServices serviceType, bool raw)
    {
        if (!raw)
        {
            console.WriteLine(
                "\nThe following score sets are available. Use these with the `score_set` property in any " +
                "quality profile defined under the top-level `quality_profiles` list.");

            console.WriteLine();
        }

        var scoreSets = guide.GetCustomFormatData(serviceType)
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

        var categories = guide.GetCustomFormatData(serviceType)
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
                "The above Custom Formats are in YAML format and ready to be copied & pasted " +
                "under the `trash_ids:` property.");
        }
    }
}
