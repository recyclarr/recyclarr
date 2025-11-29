using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ResourceProviders.Domain;

public class CategoryResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    ICustomFormatCategoryParser parser,
    ILogger log
)
{
    public IReadOnlyCollection<CustomFormatCategoryItem> GetRadarr()
    {
        log.Debug("Category: Querying Radarr categories");
        var files = registry.Get<RadarrCategoryMarkdownResource>();
        log.Debug("Category: Found {Count} category files in registry", files.Count);
        var result = files.SelectMany(parser.Parse).ToList();
        log.Debug("Category: Parsed {Count} Radarr category items", result.Count);
        return result;
    }

    public IReadOnlyCollection<CustomFormatCategoryItem> GetSonarr()
    {
        log.Debug("Category: Querying Sonarr categories");
        var files = registry.Get<SonarrCategoryMarkdownResource>();
        log.Debug("Category: Found {Count} category files in registry", files.Count);
        var result = files.SelectMany(parser.Parse).ToList();
        log.Debug("Category: Parsed {Count} Sonarr category items", result.Count);
        return result;
    }
}
