using Recyclarr.Cli.Console.Settings;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Guide.CustomFormat;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

public class CustomFormatDataLister
{
    private readonly IAnsiConsole _console;
    private readonly ICustomFormatGuideService _guide;

    public CustomFormatDataLister(IAnsiConsole console, ICustomFormatGuideService guide)
    {
        _console = console;
        _guide = guide;
    }

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
            _console.WriteLine(
                "\nThe following score sets are available. Use these with the `score_set` property in any " +
                "quality profile defined under the top-level `quality_profiles` list.");

            _console.WriteLine();
        }

        var scoreSets = _guide.GetCustomFormatData(serviceType)
            .SelectMany(x => x.TrashScores.Keys)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .Order(StringComparer.InvariantCultureIgnoreCase);

        foreach (var set in scoreSets)
        {
            _console.WriteLine(raw ? set : $" - {set}");
        }
    }

    private void ListCustomFormats(SupportedServices serviceType, bool raw)
    {
        if (!raw)
        {
            _console.WriteLine("\nList of Custom Formats in the TRaSH Guides:");
        }

        var categories = _guide.GetCustomFormatData(serviceType)
            .OrderBy(x => x.Name)
            .ToLookup(x => x.Category)
            .OrderBy(x => x.Key);

        foreach (var cat in categories)
        {
            var title = cat.Key is not null ? $"{cat.Key}" : "[No Category]";
            _console.WriteLine($"\n          # {title}");

            foreach (var cf in cat)
            {
                _console.WriteLine($"          - {cf.TrashId} # {cf.Name}");
            }
        }

        if (!raw)
        {
            _console.WriteLine(
                "\nThe above Custom Formats are in YAML format and ready to be copied & pasted " +
                "under the `trash_ids:` property.");
        }
    }
}
