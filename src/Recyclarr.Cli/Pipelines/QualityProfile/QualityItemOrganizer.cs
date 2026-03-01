using System.Diagnostics.CodeAnalysis;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal class QualityItemOrganizer
{
    private readonly List<string> _invalidItemNames = [];

    public UpdatedQualities OrganizeItems(
        IReadOnlyCollection<QualityProfileItem> items,
        QualityProfileConfig config
    )
    {
        var wanted = GetWantedItems(items, config.Qualities);
        var unwanted = GetUnwantedItems(items, wanted);
        var combined = CombineAndSortItems(config.QualitySort, wanted, unwanted);
        combined = AssignMissingGroupIds(combined);

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
    private List<QualityProfileItem> GetWantedItems(
        IReadOnlyCollection<QualityProfileItem> sourceItems,
        IReadOnlyCollection<QualityProfileQualityConfig> configQualities
    )
    {
        var updatedItems = new List<QualityProfileItem>();

        foreach (var configQuality in configQualities)
        {
            // If the nested qualities list is NOT empty, then this is considered a quality group.
            if (configQuality.Qualities.IsNotEmpty())
            {
                var existingGroup =
                    sourceItems.FindGroupByName(configQuality.Name)
                    ?? new QualityProfileItem { Name = configQuality.Name };

                var updatedGroupItems = new List<QualityProfileItem>();

                foreach (var groupQuality in configQuality.Qualities)
                {
                    AddQualityFromSource(updatedGroupItems, groupQuality);
                }

                updatedItems.Add(
                    existingGroup with
                    {
                        Allowed = configQuality.Enabled,
                        Items = updatedGroupItems,
                    }
                );

                continue;
            }

            AddQualityFromSource(updatedItems, configQuality.Name);
            continue;

            void AddQualityFromSource(ICollection<QualityProfileItem> items, string name)
            {
                var sourceItem = sourceItems.FindQualityByName(name);
                if (sourceItem is null)
                {
                    _invalidItemNames.Add(name);
                    return;
                }

                items.Add(sourceItem with { Allowed = configQuality.Enabled });
            }
        }

        return updatedItems;
    }

    private static IEnumerable<QualityProfileItem> FilterUnwantedItems(
        QualityProfileItem item,
        IReadOnlyCollection<QualityProfileItem> wantedItems
    )
    {
        // Quality
        if (item.Quality is not null)
        {
            if (wantedItems.FindQualityByName(item.Quality.Name) is null)
            {
                // Not in wanted list, so we keep
                return [item];
            }
        }
        // Group
        else
        {
            // If this is actually a quality instead of a group, this will effectively be a no-op since the Items
            // array will already be empty.
            var unwantedQualities = item.Items.Where(y =>
                wantedItems.FindQualityByName(y.Quality?.Name) is null
            );

            // If the group is in the wanted list, then we only want to add qualities inside it that are NOT wanted
            if (wantedItems.FindGroupByName(item.Name) is not null)
            {
                return unwantedQualities;
            }

            // If the group is NOT in the wanted list, keep the group and add its children (if they are not wanted)
            return
            [
                item with
                {
                    Items = unwantedQualities.Select(y => y with { Allowed = false }).ToList(),
                },
            ];
        }

        return [];
    }

    private static IEnumerable<QualityProfileItem> GetUnwantedItems(
        IEnumerable<QualityProfileItem> sourceItems,
        IReadOnlyCollection<QualityProfileItem> wantedItems
    )
    {
        return sourceItems
            .SelectMany(x => FilterUnwantedItems(x, wantedItems))
            .Select(x => x with { Allowed = false })
            // Find item groups that have less than 2 nested qualities remaining in them. Those get flattened out.
            // If Count == 0, that gets handled by the `Where()` below.
            .Select(x => x.Items.Count == 1 ? x.Items[0] : x)
            .Where(x => x is not { Quality: null, Items.Count: 0 });
    }

    private static List<QualityProfileItem> CombineAndSortItems(
        QualitySortAlgorithm sortAlgorithm,
        IEnumerable<QualityProfileItem> wantedItems,
        IEnumerable<QualityProfileItem> unwantedItems
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

    private static List<QualityProfileItem> AssignMissingGroupIds(
        List<QualityProfileItem> combinedItems
    )
    {
        // Add the IDs at the very end since we need all groups to know which IDs are taken
        var nextItemId = combinedItems.NewItemId();
        return combinedItems
            .Select(item =>
                item is { Id: null, Quality: null } ? item with { Id = nextItemId++ } : item
            )
            .ToList();
    }
}
