using System.Diagnostics.CodeAnalysis;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

public class QualityItemOrganizer
{
    private readonly List<string> _invalidItemNames = [];

    public UpdatedQualities OrganizeItems(QualityProfileDto dto, QualityProfileConfig config)
    {
        var wanted = GetWantedItems(dto.Items, config.Qualities);
        var unwanted = GetUnwantedItems(dto.Items, wanted);
        var combined = CombineAndSortItems(config.QualitySort, wanted, unwanted);

        AssignMissingGroupIds(combined);

        return new UpdatedQualities
        {
            InvalidQualityNames = _invalidItemNames,
            NumWantedItems = wanted.Count,
            Items = combined,
        };
    }

    [SuppressMessage(
        "SonarLint",
        "S1751",
        Justification = "'continue' used here is for separating local methods"
    )]
    private List<ProfileItemDto> GetWantedItems(
        IReadOnlyCollection<ProfileItemDto> dtoItems,
        IReadOnlyCollection<QualityProfileQualityConfig> configQualities
    )
    {
        var updatedItems = new List<ProfileItemDto>();

        foreach (var configQuality in configQualities)
        {
            // If the nested qualities list is NOT empty, then this is considered a quality group.
            if (configQuality.Qualities.IsNotEmpty())
            {
                var dtoGroup =
                    dtoItems.FindGroupByName(configQuality.Name)
                    ?? new ProfileItemDto { Name = configQuality.Name };

                var updatedGroupItems = new List<ProfileItemDto>();

                foreach (var groupQuality in configQuality.Qualities)
                {
                    AddQualityFromDto(updatedGroupItems, groupQuality);
                }

                updatedItems.Add(
                    dtoGroup with
                    {
                        Allowed = configQuality.Enabled,
                        Items = updatedGroupItems,
                    }
                );

                continue;
            }

            AddQualityFromDto(updatedItems, configQuality.Name);
            continue;

            void AddQualityFromDto(ICollection<ProfileItemDto> items, string name)
            {
                var dtoItem = dtoItems.FindQualityByName(name);
                if (dtoItem is null)
                {
                    _invalidItemNames.Add(name);
                    return;
                }

                items.Add(dtoItem with { Allowed = configQuality.Enabled });
            }
        }

        return updatedItems;
    }

    private static IEnumerable<ProfileItemDto> FilterUnwantedItems(
        ProfileItemDto dto,
        IReadOnlyCollection<ProfileItemDto> wantedItems
    )
    {
        // Quality
        if (dto.Quality is not null)
        {
            if (wantedItems.FindQualityByName(dto.Quality.Name) is null)
            {
                // Not in wanted list, so we keep
                return [dto];
            }
        }
        // Group
        else
        {
            // If this is actually a quality instead of a group, this will effectively be a no-op since the Items
            // array will already be empty.
            var unwantedQualities = dto.Items.Where(y =>
                wantedItems.FindQualityByName(y.Quality?.Name) is null
            );

            // If the group is in the wanted list, then we only want to add qualities inside it that are NOT wanted
            if (wantedItems.FindGroupByName(dto.Name) is not null)
            {
                return unwantedQualities;
            }

            // If the group is NOT in the wanted list, keep the group and add its children (if they are not wanted)
            return
            [
                dto with
                {
                    Items = unwantedQualities.Select(y => y with { Allowed = false }).ToList(),
                },
            ];
        }

        return Array.Empty<ProfileItemDto>();
    }

    private static IEnumerable<ProfileItemDto> GetUnwantedItems(
        IEnumerable<ProfileItemDto> dtoItems,
        IReadOnlyCollection<ProfileItemDto> wantedItems
    )
    {
        return dtoItems
            .SelectMany(x => FilterUnwantedItems(x, wantedItems))
            .Select(x => x with { Allowed = false })
            // Find item groups that have less than 2 nested qualities remaining in them. Those get flattened out.
            // If Count == 0, that gets handled by the `Where()` below.
            .Select(x => x.Items.Count == 1 ? x.Items.First() : x)
            .Where(x => x is not { Quality: null, Items.Count: 0 });
    }

    private static List<ProfileItemDto> CombineAndSortItems(
        QualitySortAlgorithm sortAlgorithm,
        IEnumerable<ProfileItemDto> wantedItems,
        IEnumerable<ProfileItemDto> unwantedItems
    )
    {
        return sortAlgorithm switch
        {
            QualitySortAlgorithm.Top => wantedItems.Concat(unwantedItems).ToList(),
            QualitySortAlgorithm.Bottom => unwantedItems.Concat(wantedItems).ToList(),
            _ => throw new ArgumentOutOfRangeException(
                $"Unsupported Quality Sort: {sortAlgorithm}"
            ),
        };
    }

    private static void AssignMissingGroupIds(IReadOnlyCollection<ProfileItemDto> combinedItems)
    {
        // Add the IDs at the very end since we need all groups to know which IDs are taken
        var nextItemId = combinedItems.NewItemId();
        foreach (var item in combinedItems.Where(item => item is { Id: null, Quality: null }))
        {
            item.Id = nextItemId++;
        }
    }
}
