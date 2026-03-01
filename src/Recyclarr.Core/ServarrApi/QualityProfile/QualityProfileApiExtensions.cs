using Recyclarr.Common.Extensions;

namespace Recyclarr.ServarrApi.QualityProfile;

public static class QualityProfileApiExtensions
{
    public static ServiceQualityProfileData ReverseItems(this ServiceQualityProfileData dto)
    {
        return dto with { Items = ReverseItemsImpl(dto.Items).AsReadOnly() };

        static ICollection<ServiceProfileItem> ReverseItemsImpl(
            IEnumerable<ServiceProfileItem> items
        ) => items.Reverse().Select(x => x with { Items = ReverseItemsImpl(x.Items) }).ToList();
    }
}
