using MoreLinq;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.QualitySize;
using Spectre.Console;

namespace Recyclarr.TrashLib.Services.Common;

public class GuideDataLister : IGuideDataLister
{
    private readonly IAnsiConsole _console;

    public GuideDataLister(IAnsiConsole console)
    {
        _console = console;
    }

    public void ListCustomFormats(IEnumerable<CustomFormatData> customFormats)
    {
        _console.WriteLine("\nList of Custom Formats in the TRaSH Guides:");

        var categories = customFormats
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

    public void ListQualities(IEnumerable<QualitySizeData> qualityData)
    {
        _console.WriteLine("\nList of Quality Definition types in the TRaSH Guides:\n");

        qualityData
            .Select(x => x.Type)
            .ForEach(x => _console.WriteLine($"  - {x}"));

        _console.WriteLine(
            "\nThe above quality definition types can be used with the `quality_definition:` property in your " +
            "recyclarr.yml file.");
    }
}
