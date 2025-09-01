namespace Recyclarr.TrashGuide.CustomFormat;

public class CustomFormatsResourceQuery(
    IEnumerable<ICustomFormatsResourceProvider> customFormatsProviders,
    IEnumerable<ICustomFormatCategoriesResourceProvider> categoriesProviders,
    ICustomFormatLoader cfLoader
) : ICustomFormatsResourceQuery
{
    private readonly Lazy<
        IReadOnlyDictionary<SupportedServices, ICollection<CustomFormatData>>
    > _cache = new(() =>
    {
        var result = new Dictionary<SupportedServices, ICollection<CustomFormatData>>();

        // Get category data from all providers (computed once for all services)
        var categoryData = categoriesProviders
            .SelectMany(provider => provider.GetCategoryData())
            .ToList();

        foreach (var serviceType in Enum.GetValues<SupportedServices>())
        {
            // Get custom format directories from all providers for this service
            var customFormatPaths = customFormatsProviders.SelectMany(provider =>
                provider.GetCustomFormatPaths(serviceType)
            );

            // For now, we need to adapt to the existing CustomFormatLoader interface
            // This will need refactoring when we integrate category parsing properly
            // TODO: Integrate categoryData with CustomFormatLoader when category parsing is implemented
            _ = categoryData; // Acknowledge variable to suppress warning
            var paths = new CustomFormatPaths(
                customFormatPaths.ToList(),
                null! // We'll handle categories separately for now
            );

            result[serviceType] = cfLoader.LoadAllCustomFormatsAtPaths(
                paths.CustomFormatDirectories,
                paths.CollectionOfCustomFormatsMarkdown
            );
        }

        return result;
    });

    public ICollection<CustomFormatData> GetCustomFormatData(SupportedServices serviceType) =>
        _cache.Value[serviceType];
}
