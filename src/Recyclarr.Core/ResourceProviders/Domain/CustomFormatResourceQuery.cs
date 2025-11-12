using Recyclarr.Common.Extensions;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ResourceProviders.Domain;

public class CustomFormatResourceQuery(
    IResourcePathRegistry registry,
    JsonResourceLoader loader,
    CategoryResourceQuery categoryQuery
)
{
    public IReadOnlyCollection<RadarrCustomFormatResource> GetRadarr()
    {
        return GetCustomFormats<RadarrCustomFormatResource>(categoryQuery.GetRadarr());
    }

    public IReadOnlyCollection<SonarrCustomFormatResource> GetSonarr()
    {
        return GetCustomFormats<SonarrCustomFormatResource>(categoryQuery.GetSonarr());
    }

    private IReadOnlyCollection<TResource> GetCustomFormats<TResource>(
        IReadOnlyCollection<CustomFormatCategoryItem> categories
    )
        where TResource : CustomFormatResource
    {
        var files = registry.GetFiles<TResource>();
        var loaded = loader.Load<TResource>(files);

        return loaded
            .Select(tuple => AssignCategory(tuple, categories))
            .GroupBy(cf => cf.TrashId)
            .Select(g => g.Last())
            .ToList();
    }

    private static TResource AssignCategory<TResource>(
        (TResource Resource, System.IO.Abstractions.IFileInfo SourceFile) tuple,
        IEnumerable<CustomFormatCategoryItem> categories
    )
        where TResource : CustomFormatResource
    {
        var (cf, sourceFile) = tuple;
        var match = categories.FirstOrDefault(cat =>
            cat.CfName.EqualsIgnoreCase(cf.Name)
            || cat.CfAnchor.EqualsIgnoreCase(Path.GetFileNameWithoutExtension(sourceFile.Name))
        );

        return cf with
        {
            Category = match?.CategoryName,
        };
    }
}
