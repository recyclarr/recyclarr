using Recyclarr.TrashLib.Config;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Guide;

public class CustomFormatDataLister
{
    private readonly IAnsiConsole _console;
    private readonly ICustomFormatGuideService _guide;

    public CustomFormatDataLister(IAnsiConsole console, ICustomFormatGuideService guide)
    {
        _console = console;
        _guide = guide;
    }

    public void ListCustomFormats(SupportedServices serviceType)
    {
        _console.WriteLine("\nList of Custom Formats in the TRaSH Guides:");

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

        _console.WriteLine(
            "\nThe above Custom Formats are in YAML format and ready to be copied & pasted " +
            "under the `trash_ids:` property.");
    }
}
