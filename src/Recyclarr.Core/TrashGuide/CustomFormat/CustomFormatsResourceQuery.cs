using System.IO.Abstractions;

namespace Recyclarr.TrashGuide.CustomFormat;

public class CustomFormatsResourceQuery(
    IReadOnlyCollection<ICustomFormatsResourceProvider> customFormatsProviders,
    IReadOnlyCollection<ICustomFormatCategoriesResourceProvider> categoriesProviders,
    ICustomFormatLoader cfLoader
) : ICustomFormatsResourceQuery
{
    private readonly Dictionary<SupportedServices, ICollection<CustomFormatData>> _cache = [];

    public ICollection<CustomFormatData> GetCustomFormatData(SupportedServices serviceType)
    {
        if (_cache.TryGetValue(serviceType, out var cached))
        {
            return cached;
        }

        // Get custom format directories from all providers for this service
        var customFormatPaths = customFormatsProviders.SelectMany(provider =>
            provider.GetCustomFormatPaths(serviceType)
        );

        // Get the appropriate category markdown file for this service
        var categoryFile = categoriesProviders
            .Select(provider => provider.GetCategoryMarkdownFile(serviceType))
            .FirstOrDefault(file => file != null);

        if (categoryFile == null)
        {
            throw new InvalidOperationException(
                $"No category markdown file found for service {serviceType}"
            );
        }

        var result = cfLoader.LoadAllCustomFormatsAtPaths(customFormatPaths, categoryFile);
        _cache[serviceType] = result;
        return result;
    }
}
