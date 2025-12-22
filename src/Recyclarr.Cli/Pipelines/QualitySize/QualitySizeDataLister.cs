using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.QualitySize;

internal class QualitySizeDataLister(IAnsiConsole console, QualitySizeResourceQuery guide)
{
    public void ListQualities(SupportedServices serviceType)
    {
        console.WriteLine("\nList of Quality Definition types in the TRaSH Guides:\n");

        var qualitySizes = guide.Get(serviceType);

        qualitySizes.Select(x => x.Type).ForEach(x => console.WriteLine($"  - {x}"));

        console.WriteLine(
            "\nThe above quality definition types can be used with the `quality_definition:` property in your "
                + "recyclarr.yml file."
        );
    }
}
