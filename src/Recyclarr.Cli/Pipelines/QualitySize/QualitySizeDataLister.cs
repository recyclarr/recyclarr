using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.QualitySize;

public class QualitySizeDataLister(IAnsiConsole console, IQualitySizeGuideService guide)
{
    public void ListQualities(SupportedServices serviceType)
    {
        console.WriteLine("\nList of Quality Definition types in the TRaSH Guides:\n");

        guide
            .GetQualitySizeData(serviceType)
            .Select(x => x.Type)
            .ForEach(x => console.WriteLine($"  - {x}"));

        console.WriteLine(
            "\nThe above quality definition types can be used with the `quality_definition:` property in your "
                + "recyclarr.yml file."
        );
    }
}
