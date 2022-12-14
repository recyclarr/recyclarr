using CliFx.Infrastructure;
using JetBrains.Annotations;
using MoreLinq;
using Recyclarr.TrashLib.Services.Common;

namespace Recyclarr.TrashLib.Services.Radarr;

[UsedImplicitly]
public class RadarrGuideDataLister : IRadarrGuideDataLister
{
    private readonly IConsole _console;
    private readonly IRadarrGuideService _guide;
    private readonly IGuideDataLister _guideLister;

    public RadarrGuideDataLister(
        IConsole console,
        IRadarrGuideService guide,
        IGuideDataLister guideLister)
    {
        _console = console;
        _guide = guide;
        _guideLister = guideLister;
    }

    public void ListCustomFormats() => _guideLister.ListCustomFormats(_guide.GetCustomFormatData());

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
