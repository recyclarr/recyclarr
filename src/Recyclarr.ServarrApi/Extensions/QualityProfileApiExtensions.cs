using Recyclarr.Common.Extensions;
using Recyclarr.ServarrApi.Dto;

namespace Recyclarr.ServarrApi.Extensions;

public static class QualityProfileApiExtensions
{
    public static QualityProfileDto ReverseItems(this QualityProfileDto dto)
    {
        return dto with {Items = ReverseItemsImpl(dto.Items).AsReadOnly()};

        static ICollection<ProfileItemDto> ReverseItemsImpl(IEnumerable<ProfileItemDto> items)
            => items.Reverse().Select(x => x with {Items = ReverseItemsImpl(x.Items)}).ToList();
    }
}
