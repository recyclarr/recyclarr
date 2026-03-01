using Recyclarr.Common.Extensions;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal static class QualityProfileExtensions
{
    private static IEnumerable<QualityProfileItem> FlattenItems(
        IEnumerable<QualityProfileItem> items
    )
    {
        return items.Flatten(x => x.Items);
    }

    public static IEnumerable<QualityProfileItem> FlattenQualities(
        this IEnumerable<QualityProfileItem> items
    )
    {
        return FlattenItems(items).Where(x => x.Quality is not null);
    }

    public static QualityProfileItem? FindGroupById(
        this IEnumerable<QualityProfileItem> items,
        int? id
    )
    {
        if (id is null)
        {
            return null;
        }

        return FlattenItems(items).Where(x => x.Quality is null).FirstOrDefault(x => x.Id == id);
    }

    public static QualityProfileItem? FindGroupByName(
        this IEnumerable<QualityProfileItem> items,
        string? name
    )
    {
        if (name is null)
        {
            return null;
        }

        return FlattenItems(items)
            .Where(x => x.Quality is null)
            .FirstOrDefault(x => x.Name.EqualsIgnoreCase(name));
    }

    public static QualityProfileItem? FindQualityById(
        this IEnumerable<QualityProfileItem> items,
        int? id
    )
    {
        if (id is null)
        {
            return null;
        }

        return FlattenItems(items)
            .Where(x => x.Quality is not null)
            .FirstOrDefault(x => x.Quality!.Id == id);
    }

    public static QualityProfileItem? FindQualityByName(
        this IEnumerable<QualityProfileItem> items,
        string? name
    )
    {
        if (name is null)
        {
            return null;
        }

        return FlattenItems(items)
            .Where(x => x.Quality is not null)
            .FirstOrDefault(x => x.Quality!.Name.EqualsIgnoreCase(name));
    }

    private static IEnumerable<(string? Name, int? Id)> GetEligibleCutoffs(
        IEnumerable<QualityProfileItem> items
    )
    {
        return items
            .Where(x => x.Allowed is true)
            .Select(x => x.Quality is null ? (x.Name, x.Id) : (x.Quality.Name, x.Quality.Id))
            .Where(x => x.Name is not null);
    }

    public static int? FindCutoff(this IEnumerable<QualityProfileItem> items, string? name)
    {
        if (name is null)
        {
            return null;
        }

        var result = GetEligibleCutoffs(items).FirstOrDefault(x => x.Name.EqualsIgnoreCase(name));

        return result.Id;
    }

    public static string? FindCutoff(this IEnumerable<QualityProfileItem> items, int? id)
    {
        if (id is null)
        {
            return null;
        }

        var result = GetEligibleCutoffs(items).FirstOrDefault(x => x.Id == id);

        return result.Name;
    }

    public static int? FirstCutoffId(this IEnumerable<QualityProfileItem> items)
    {
        return GetEligibleCutoffs(items).FirstOrDefault().Id;
    }

    public static int NewItemId(this IEnumerable<QualityProfileItem> items)
    {
        // This implementation is based on how the Radarr frontend calculates IDs.
        // This calculation will be applied to new quality item groups.
        // See `getQualityItemGroupId()` here:
        // https://github.com/Radarr/Radarr/blob/c214a6b67bf747e02462066cd1c6db7bc06db1f0/frontend/src/Settings/Profiles/Quality/EditQualityProfileModalContentConnector.js#L11C8-L11C8
        var maxExisting = FlattenItems(items).Select(x => x.Id).NotNull().DefaultIfEmpty(0).Max();

        return Math.Max(1000, maxExisting) + 1;
    }
}
