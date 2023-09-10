using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Guide.QualitySize;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.QualitySize;

public class QualitySizeDataLister
{
    private readonly IAnsiConsole _console;
    private readonly IQualitySizeGuideService _guide;

    public QualitySizeDataLister(
        IAnsiConsole console,
        IQualitySizeGuideService guide)
    {
        _console = console;
        _guide = guide;
    }

    public void ListQualities(SupportedServices serviceType)
    {
        _console.WriteLine("\nList of Quality Definition types in the TRaSH Guides:\n");

        _guide.GetQualitySizeData(serviceType)
            .Select(x => x.Type)
            .ForEach(x => _console.WriteLine($"  - {x}"));

        _console.WriteLine(
            "\nThe above quality definition types can be used with the `quality_definition:` property in your " +
            "recyclarr.yml file.");
    }
}
