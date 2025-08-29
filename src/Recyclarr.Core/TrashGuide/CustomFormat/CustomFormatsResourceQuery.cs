namespace Recyclarr.TrashGuide.CustomFormat;

public class CustomFormatsResourceQuery(
    IEnumerable<ICustomFormatsResourceProvider> customFormatsProviders,
    IEnumerable<ICustomFormatCategoriesResourceProvider> categoriesProviders,
    ICustomFormatLoader cfLoader
) : ICustomFormatsResourceQuery
{
    private readonly Dictionary<SupportedServices, ICollection<CustomFormatData>> _cache = new();

    public ICollection<CustomFormatData> GetCustomFormatData(SupportedServices serviceType)
    {
        if (_cache.TryGetValue(serviceType, out var cfData))
        {
            return cfData;
        }

        // Get custom format directories from all providers
        var customFormatPaths = customFormatsProviders.SelectMany(provider =>
            provider.GetCustomFormatPaths(serviceType)
        );

        // Get category data from all providers
        var categoryData = categoriesProviders
            .SelectMany(provider => provider.GetCategoryData())
            .ToList();

        // For now, we need to adapt to the existing CustomFormatLoader interface
        // This will need refactoring when we integrate category parsing properly
        // TODO: Integrate categoryData with CustomFormatLoader when category parsing is implemented
        _ = categoryData; // Acknowledge variable to suppress warning
        var paths = new CustomFormatPaths(
            customFormatPaths.ToList(),
            null! // We'll handle categories separately for now
        );

        return _cache[serviceType] = cfLoader.LoadAllCustomFormatsAtPaths(
            paths.CustomFormatDirectories,
            paths.CollectionOfCustomFormatsMarkdown
        );
    }
}
