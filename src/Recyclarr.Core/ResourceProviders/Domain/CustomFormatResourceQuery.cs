using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ResourceProviders.Domain;

public class CustomFormatResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    JsonResourceLoader loader,
    CategoryResourceQuery categoryQuery,
    ILogger log
)
{
    public IReadOnlyList<RadarrCustomFormatResource> GetRadarr()
    {
        log.Debug("CustomFormat: Querying Radarr custom formats");
        var result = GetCustomFormats<RadarrCustomFormatResource>(categoryQuery.GetRadarr());
        log.Debug("CustomFormat: Retrieved {Count} Radarr custom formats", result.Count);
        return result;
    }

    public IReadOnlyList<SonarrCustomFormatResource> GetSonarr()
    {
        log.Debug("CustomFormat: Querying Sonarr custom formats");
        var result = GetCustomFormats<SonarrCustomFormatResource>(categoryQuery.GetSonarr());
        log.Debug("CustomFormat: Retrieved {Count} Sonarr custom formats", result.Count);
        return result;
    }

    private List<TResource> GetCustomFormats<TResource>(
        IReadOnlyCollection<CustomFormatCategoryItem> categories
    )
        where TResource : CustomFormatResource
    {
        var files = registry.Get<TResource>();
        log.Debug("CustomFormat: Found {Count} CF files in registry", files.Count);

        var loaded = loader.Load<TResource>(files, GlobalJsonSerializerSettings.Guide);

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
