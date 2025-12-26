using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.QualitySize;

internal class QualitySizeDataLister(
    ILogger log,
    IAnsiConsole console,
    QualitySizeResourceQuery guide
)
{
    public void ListQualities(SupportedServices serviceType)
    {
        console.WriteLine("\nList of Quality Definition types in the TRaSH Guides:\n");

        var qualitySizes = guide.Get(serviceType).ToList();

        log.Debug(
            "Found {Count} quality definition types for {Service}",
            qualitySizes.Count,
            serviceType
        );

        qualitySizes.Select(x => x.Type).ForEach(x => console.WriteLine($"  - {x}"));

        console.WriteLine(
            "\nUse these with the `quality_definition:` property in your recyclarr.yml file."
        );
    }
}
