using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ResourceProviders.Domain;

public class CategoryResourceQuery(
    IResourcePathRegistry registry,
    ICustomFormatCategoryParser parser
)
{
    public IReadOnlyCollection<CustomFormatCategoryItem> GetRadarr()
    {
        var files = registry.GetFiles<RadarrCategoryMarkdownResource>();
        return files.SelectMany(parser.Parse).ToList();
    }

    public IReadOnlyCollection<CustomFormatCategoryItem> GetSonarr()
    {
        var files = registry.GetFiles<SonarrCategoryMarkdownResource>();
        return files.SelectMany(parser.Parse).ToList();
    }
}
