using CliFx.Infrastructure;
using JetBrains.Annotations;
using MoreLinq;
using TrashLib.Services.Radarr.CustomFormat.Guide;

namespace TrashLib.Services.Radarr.CustomFormat;

[UsedImplicitly]
public class RadarrGuideDataLister : IRadarrGuideDataLister
{
    private readonly IConsole _console;
    private readonly IRadarrGuideService _guide;

    public RadarrGuideDataLister(IConsole console, IRadarrGuideService guide)
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

    public void ListQualities()
    {
        _console.Output.WriteLine("\nList of Quality Definition types in the TRaSH Guides:\n");

        _guide.GetQualities()
            .Select(x => x.Type)
            .ForEach(x => _console.Output.WriteLine($"  - {x}"));

        _console.Output.WriteLine(
            "\nThe above quality definition types can be used with the `quality_definition:` property in your " +
            "recyclarr.yml file.");
    }
}
