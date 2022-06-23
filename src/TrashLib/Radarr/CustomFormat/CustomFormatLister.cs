using CliFx.Infrastructure;
using JetBrains.Annotations;
using TrashLib.Radarr.CustomFormat.Guide;

namespace TrashLib.Radarr.CustomFormat;

[UsedImplicitly]
public class CustomFormatLister : ICustomFormatLister
{
    private readonly IConsole _console;
    private readonly IRadarrGuideService _guide;

    public CustomFormatLister(IConsole console, IRadarrGuideService guide)
    {
        _console = console;
        _guide = guide;
    }

    public void ListCustomFormats()
    {
        _console.Output.WriteLine("\nList of Custom Formats in the TRaSH Guides:\n");

        var profilesFromGuide = _guide.GetCustomFormatData();
        foreach (var profile in profilesFromGuide)
        {
            _console.Output.WriteLine($"          - {profile.TrashId} # {profile.Name}");
        }

        _console.Output.WriteLine(
            "\nThe above Custom Formats are in YAML format and ready to be copied & pasted " +
            "under the `trash_ids:` property.");
    }
}
