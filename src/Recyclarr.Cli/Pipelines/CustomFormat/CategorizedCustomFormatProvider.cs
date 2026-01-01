using Recyclarr.Common.Extensions;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal record CategorizedCustomFormat(CustomFormatResource Resource, string? Category);

internal class CategorizedCustomFormatProvider(
    CustomFormatResourceQuery cfQuery,
    CategoryResourceQuery categoryQuery
)
{
    public IReadOnlyList<CategorizedCustomFormat> Get(SupportedServices serviceType)
    {
        var cfs = cfQuery.Get(serviceType);
        var categories = categoryQuery.Get(serviceType);
        return cfs.Select(cf => new CategorizedCustomFormat(cf, ResolveCategory(cf, categories)))
            .ToList();
    }

    private static string? ResolveCategory(
        CustomFormatResource cf,
        IReadOnlyCollection<CustomFormatCategoryItem> categories
    )
    {
        return categories
            .FirstOrDefault(cat =>
                cat.CfName.EqualsIgnoreCase(cf.Name)
                || cat.CfAnchor.EqualsIgnoreCase(
                    Path.GetFileNameWithoutExtension(cf.SourceFile?.Name ?? "")
                )
            )
            ?.CategoryName;
    }
}
