using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ResourceProviders.Domain;

public class CategoryResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    ICustomFormatCategoryParser parser
)
{
    public IReadOnlyCollection<CustomFormatCategoryItem> GetRadarr()
    {
        var files = registry.Get<RadarrCategoryMarkdownResource>();
        return files.SelectMany(parser.Parse).ToList();
    }

    public IReadOnlyCollection<CustomFormatCategoryItem> GetSonarr()
    {
        var files = registry.Get<SonarrCategoryMarkdownResource>();
        return files.SelectMany(parser.Parse).ToList();
    }
}
