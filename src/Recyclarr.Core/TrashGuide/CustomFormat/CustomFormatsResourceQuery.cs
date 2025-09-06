using System.IO.Abstractions;

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

        var allFormatsWithSources = new List<(CustomFormatData Format, string Source)>();

        foreach (var provider in customFormatsProviders)
        {
            var providerPaths = provider.GetCustomFormatPaths(serviceType);
            var sourceDescription = provider.GetSourceDescription();

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

            foreach (var format in providerFormats)
            {
                allFormatsWithSources.Add((format, sourceDescription));
            }
        }

        // Group by TrashId to detect duplicates
        var groupedByTrashId = allFormatsWithSources.GroupBy(item => item.Format.TrashId).ToList();

        var cleanFormats = new List<CustomFormatData>();
        var duplicates = new List<DuplicateCustomFormatInfo>();

        foreach (var group in groupedByTrashId)
        {
            var trashId = group.Key;
            var items = group.ToList();

            if (items.Count > 1)
            {
                // Duplicate found - collect metadata
                var names = items.Select(item => item.Format.Name).Distinct().ToList();
                var sources = items.Select(item => item.Source).Distinct().ToList();

                duplicates.Add(new DuplicateCustomFormatInfo(trashId, names, sources));

                // Use first occurrence for clean data (existing behavior)
                cleanFormats.Add(items.First().Format);
            }
            else
            {
                // No duplicate - add to clean data
                cleanFormats.Add(items.First().Format);
            }
        }

        var result = new CustomFormatDataResult(cleanFormats, duplicates);
        _cache[serviceType] = result;
        return result;
    }
}
