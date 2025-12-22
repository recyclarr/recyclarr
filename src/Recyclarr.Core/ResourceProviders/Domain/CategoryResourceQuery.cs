using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ResourceProviders.Domain;

public class CategoryResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    ICustomFormatCategoryParser parser,
    ILogger log
)
{
    public IReadOnlyCollection<CustomFormatCategoryItem> Get(SupportedServices serviceType)
    {
        return serviceType switch
        {
            SupportedServices.Radarr => GetCategories<RadarrCategoryMarkdownResource>(serviceType),
            SupportedServices.Sonarr => GetCategories<SonarrCategoryMarkdownResource>(serviceType),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };
    }

    private List<CustomFormatCategoryItem> GetCategories<TResource>(SupportedServices serviceType)
        where TResource : CategoryMarkdownResource
    {
        log.Debug("Category: Querying {Service} categories", serviceType);
        var files = registry.Get<TResource>();
        log.Debug("Category: Found {Count} category files in registry", files.Count);
        var result = files.SelectMany(parser.Parse).ToList();
        log.Debug("Category: Parsed {Count} {Service} category items", result.Count, serviceType);
        return result;
    }
}
