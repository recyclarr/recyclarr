namespace Recyclarr.TrashGuide.CustomFormat;

public class CustomFormatsResourceQuery(
    IReadOnlyCollection<ICustomFormatsResourceProvider> customFormatsProviders,
    IReadOnlyCollection<ICustomFormatCategoriesResourceProvider> categoriesProviders,
    ICustomFormatLoader cfLoader,
    ILogger log
) : ICustomFormatsResourceQuery
{
    private readonly Dictionary<SupportedServices, CustomFormatDataResult> _cache = [];

    public CustomFormatDataResult GetCustomFormatData(SupportedServices serviceType)
    {
        if (_cache.TryGetValue(serviceType, out var cached))
        {
            return cached;
        }

        // Get custom format directories from all providers for this service, tracking sources
        log.Debug(
            "CustomFormatsResourceQuery.GetCustomFormatData called for {ServiceType}",
            serviceType
        );

        var allFormatsWithSources = new List<CustomFormatData>();

        foreach (var provider in customFormatsProviders)
        {
            var providerPaths = provider.GetCustomFormatPaths(serviceType);

            // Get category file for this provider
            var categoryFile = categoriesProviders
                .Select(p => p.GetCategoryMarkdownFile(serviceType))
                .FirstOrDefault(file => file != null);

            if (categoryFile == null)
            {
                throw new InvalidOperationException(
                    $"No category markdown file found for service {serviceType}"
                );
            }

            var providerFormats = cfLoader.LoadAllCustomFormatsAtPaths(providerPaths, categoryFile);

            allFormatsWithSources.AddRange(providerFormats);
        }

        // Apply precedence-based approach: first provider wins for each TrashId
        var cleanFormats = allFormatsWithSources
            .GroupBy(item => item.TrashId)
            .Select(group => group.First()) // First occurrence takes precedence
            .ToList();

        // No duplicate tracking needed - multiple providers are expected for redundancy/fallback
        var result = new CustomFormatDataResult(cleanFormats);
        _cache[serviceType] = result;
        return result;
    }
}
