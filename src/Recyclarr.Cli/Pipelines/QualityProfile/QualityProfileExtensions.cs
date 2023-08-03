using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

public static class QualityProfileExtensions
{
    private static IEnumerable<ProfileItemDto> FlattenItems(IEnumerable<ProfileItemDto> items)
    {
        return items.Flatten(x => x.Items);
    }

    public static ProfileItemDto? FindGroupById(this IEnumerable<ProfileItemDto> items, int? id)
    {
        if (id is null)
        {
            return null;
        }

        return FlattenItems(items)
            .Where(x => x.Quality is null)
            .FirstOrDefault(x => x.Id == id);
    }

    public static ProfileItemDto? FindGroupByName(this IEnumerable<ProfileItemDto> items, string? name)
    {
        if (name is null)
        {
            return null;
        }

        return FlattenItems(items)
            .Where(x => x.Quality is null)
            .FirstOrDefault(x => x.Name.EqualsIgnoreCase(name));
    }

    public static ProfileItemDto? FindQualityById(this IEnumerable<ProfileItemDto> items, int? id)
    {
        if (id is null)
        {
            return null;
        }

        return FlattenItems(items)
            .Where(x => x.Quality is not null)
            .FirstOrDefault(x => x.Quality!.Id == id);
    }

    public static ProfileItemDto? FindQualityByName(this IEnumerable<ProfileItemDto> items, string? name)
    {
        if (name is null)
        {
            return null;
        }

        return FlattenItems(items)
            .Where(x => x.Quality is not null)
            .FirstOrDefault(x => x.Quality!.Name.EqualsIgnoreCase(name));
    }

    private static IEnumerable<(string? Name, int? Id)> GetEligibleCutoffs(IEnumerable<ProfileItemDto> items)
    {
        return items
            .Where(x => x.Allowed is true)
            .Select(x => x.Quality is null ? (x.Name, x.Id) : (x.Quality.Name, x.Quality.Id))
            .Where(x => x.Name is not null);
    }

    public static int? FindCutoff(this IEnumerable<ProfileItemDto> items, string? name)
    {
        if (name is null)
        {
            return null;
        }

        var result = GetEligibleCutoffs(items)
            .FirstOrDefault(x => x.Name.EqualsIgnoreCase(name));

        return result.Id;
    }

    public static string? FindCutoff(this IEnumerable<ProfileItemDto> items, int? id)
    {
        if (id is null)
        {
            return null;
        }

        var result = GetEligibleCutoffs(items)
            .FirstOrDefault(x => x.Id == id);

        return result.Name;
    }

    public static int NewItemId(this IEnumerable<ProfileItemDto> items)
    {
        // This implementation is based on how the Radarr frontend calculates IDs.
        // This calculation will be applied to new quality item groups.
        // See `getQualityItemGroupId()` here:
        // https://github.com/Radarr/Radarr/blob/c214a6b67bf747e02462066cd1c6db7bc06db1f0/frontend/src/Settings/Profiles/Quality/EditQualityProfileModalContentConnector.js#L11C8-L11C8
        var maxExisting = FlattenItems(items)
            .Select(x => x.Id)
            .NotNull()
            .DefaultIfEmpty(0)
            .Max();

        return Math.Max(1000, maxExisting) + 1;
    }

    public static UpdatedQualities BuildUpdatedQualityItems(
        this QualityProfileDto dto,
        QualityProfileConfig configProfile)
    {
        var organizer = new QualityItemOrganizer();
        return organizer.OrganizeItems(dto, configProfile);
    }

    public static QualityProfileDto ReverseItems(this QualityProfileDto dto)
    {
        static ICollection<ProfileItemDto> ReverseItemsImpl(IEnumerable<ProfileItemDto> items)
            => items.Reverse().Select(x => x with {Items = ReverseItemsImpl(x.Items)}).ToList();

        return dto with {Items = ReverseItemsImpl(dto.Items).AsReadOnly()};
    }
}
