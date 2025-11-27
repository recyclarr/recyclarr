using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ResourceProviders.Domain;

public class CustomFormatResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    JsonResourceLoader loader,
    CategoryResourceQuery categoryQuery
)
{
    public IReadOnlyList<RadarrCustomFormatResource> GetRadarr()
    {
        return GetCustomFormats<RadarrCustomFormatResource>(categoryQuery.GetRadarr());
    }

    public IReadOnlyList<SonarrCustomFormatResource> GetSonarr()
    {
        return GetCustomFormats<SonarrCustomFormatResource>(categoryQuery.GetSonarr());
    }

    private List<TResource> GetCustomFormats<TResource>(
        IReadOnlyCollection<CustomFormatCategoryItem> categories
    )
        where TResource : CustomFormatResource
    {
        var files = registry.Get<TResource>();
        var loaded = loader.Load<TResource>(files);

        return loaded
            .Select(tuple => AssignCategory(tuple, categories))
            .GroupBy(cf => cf.TrashId)
            .Select(g => g.Last())
            .ToList();
    }

    private static TResource AssignCategory<TResource>(
        (TResource Resource, IFileInfo SourceFile) tuple,
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
