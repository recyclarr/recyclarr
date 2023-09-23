using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

public class QualityItemOrganizer
{
    private readonly List<string> _invalidItemNames = new();

    public UpdatedQualities OrganizeItems(QualityProfileDto dto, QualityProfileConfig config)
    {
        var wanted = ProcessWantedItems(dto.Items, config.Qualities);
        var unwanted = ProcessUnwantedItems(dto.Items, wanted);
        var combined = CombineAndSortItems(config.QualitySort, wanted, unwanted);

        AssignMissingGroupIds(combined);

        return new UpdatedQualities
        {
            InvalidQualityNames = _invalidItemNames,
            NumWantedItems = wanted.Count,
            Items = combined
        };
    }

    [SuppressMessage("SonarLint", "S1751", Justification =
        "'continue' used here is for separating local methods")]
    private List<ProfileItemDto> ProcessWantedItems(
        IReadOnlyCollection<ProfileItemDto> dtoItems,
        IReadOnlyCollection<QualityProfileQualityConfig> configQualities)
    {
        var updatedItems = new List<ProfileItemDto>();

        foreach (var configQuality in configQualities)
        {
            // If the nested qualities list is NOT empty, then this is considered a quality group.
            if (configQuality.Qualities.IsNotEmpty())
            {
                var dtoGroup = dtoItems.FindGroupByName(configQuality.Name) ?? new ProfileItemDto
                {
                    Name = configQuality.Name
                };

                var updatedGroupItems = new List<ProfileItemDto>();

                foreach (var groupQuality in configQuality.Qualities)
                {
                    AddQualityFromDto(updatedGroupItems, groupQuality);
                }

                updatedItems.Add(dtoGroup with
                {
                    Allowed = configQuality.Enabled,
                    Items = updatedGroupItems
                });

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

                items.Add(dtoItem with {Allowed = configQuality.Enabled});
            }
        }

        return updatedItems;
    }

    private static IEnumerable<ProfileItemDto> ProcessUnwantedItems(
        IEnumerable<ProfileItemDto> dtoItems,
        IReadOnlyCollection<ProfileItemDto> wantedItems)
    {
        // Find remaining items in the DTO that were *not* handled by the user's config.
        return dtoItems
            .Where(x => !ExistsInWantedItems(wantedItems, x))
            .Select(x => x with
            {
                Allowed = false,

                // If this is actually a quality instead of a group, this will effectively be a no-op since the Items
                // array will already be empty.
                Items = x.Items
                    .Where(y => wantedItems.FindQualityByName(y.Quality?.Name) is null)
                    .Select(y => y with {Allowed = false})
                    .ToList()
            })
            .Where(x => x is not {Quality: null, Items.Count: 0});
    }

    private static List<ProfileItemDto> CombineAndSortItems(
        QualitySortAlgorithm sortAlgorithm,
        IEnumerable<ProfileItemDto> wantedItems,
        IEnumerable<ProfileItemDto> unwantedItems)
    {
        return sortAlgorithm switch
        {
            QualitySortAlgorithm.Top => wantedItems.Concat(unwantedItems).ToList(),
            QualitySortAlgorithm.Bottom => unwantedItems.Concat(wantedItems).ToList(),
            _ => throw new ArgumentOutOfRangeException($"Unsupported Quality Sort: {sortAlgorithm}")
        };
    }

    private static void AssignMissingGroupIds(IReadOnlyCollection<ProfileItemDto> combinedItems)
    {
        // Add the IDs at the very end since we need all groups to know which IDs are taken
        var nextItemId = combinedItems.NewItemId();
        foreach (var item in combinedItems.Where(item => item is {Id: null, Quality: null}))
        {
            item.Id = nextItemId++;
        }
    }

    private static bool ExistsInWantedItems(IEnumerable<ProfileItemDto> wantedItems, ProfileItemDto dto)
    {
        var existingItem = dto.Quality is null
            ? wantedItems.FindGroupByName(dto.Name)
            : wantedItems.FindQualityByName(dto.Quality.Name);

        return existingItem is not null;
    }
}
