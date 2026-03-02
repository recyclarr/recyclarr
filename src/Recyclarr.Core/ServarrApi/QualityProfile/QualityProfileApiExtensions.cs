using RadarrApi = Recyclarr.Api.Radarr;
using SonarrApi = Recyclarr.Api.Sonarr;

namespace Recyclarr.ServarrApi.QualityProfile;

internal static class QualityProfileApiExtensions
{
    public static SonarrApi.QualityProfileResource ReverseItems(
        this SonarrApi.QualityProfileResource dto
    )
    {
        dto.Items = ReverseItemsImpl(dto.Items ?? []);
        return dto;

        static ICollection<SonarrApi.QualityProfileQualityItemResource> ReverseItemsImpl(
            IEnumerable<SonarrApi.QualityProfileQualityItemResource> items
        ) =>
            items
                .Reverse()
                .Select(x =>
                {
                    x.Items = ReverseItemsImpl(x.Items ?? []);
                    return x;
                })
                .ToList();
    }

    public static RadarrApi.QualityProfileResource ReverseItems(
        this RadarrApi.QualityProfileResource dto
    )
    {
        dto.Items = ReverseItemsImpl(dto.Items ?? []);
        return dto;

        static ICollection<RadarrApi.QualityProfileQualityItemResource> ReverseItemsImpl(
            IEnumerable<RadarrApi.QualityProfileQualityItemResource> items
        ) =>
            items
                .Reverse()
                .Select(x =>
                {
                    x.Items = ReverseItemsImpl(x.Items ?? []);
                    return x;
                })
                .ToList();
    }
}
