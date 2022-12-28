using CliFx.Infrastructure;
using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.Common;

public class GuideDataLister : IGuideDataLister
{
    private readonly IConsole _console;

    public GuideDataLister(IConsole console)
    {
        _console = console;
    }

    public void ListCustomFormats(IEnumerable<CustomFormatData> customFormats)
    {
        _console.Output.WriteLine("\nList of Custom Formats in the TRaSH Guides:");

        var categories = customFormats
            .OrderBy(x => x.Name)
            .ToLookup(x => x.Category)
            .OrderBy(x => x.Key);

        foreach (var cat in categories)
        {
            var title = cat.Key is not null ? $"{cat.Key}" : "[No Category]";
            _console.Output.WriteLine($"\n          # {title}");

            foreach (var cf in cat)
            {
                _console.Output.WriteLine($"          - {cf.TrashId} # {cf.Name}");
            }
        }

        _console.Output.WriteLine(
            "\nThe above Custom Formats are in YAML format and ready to be copied & pasted " +
            "under the `trash_ids:` property.");
    }
}
