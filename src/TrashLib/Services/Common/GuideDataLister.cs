using CliFx.Infrastructure;
using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.Common;

public class GuideDataLister : IGuideDataLister
{
    private readonly IConsole _console;

    public GuideDataLister(IConsole console)
    {
        _console = console;
    }

    public void ListCustomFormats(IEnumerable<CustomFormatData> customFormats)
    {
        _console.Output.WriteLine("\nList of Custom Formats in the TRaSH Guides:\n");

        foreach (var cf in customFormats)
        {
            _console.Output.WriteLine($"          - {cf.TrashId} # {cf.Name}");
        }

        _console.Output.WriteLine(
            "\nThe above Custom Formats are in YAML format and ready to be copied & pasted " +
            "under the `trash_ids:` property.");
    }
}
