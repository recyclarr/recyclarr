using Recyclarr.Common.Extensions;
using Recyclarr.Servarr.QualityProfile;
using Riok.Mapperly.Abstractions;
using SonarrApi = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.ServarrApi.QualityProfile;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class SonarrV5QualityProfileMapper
{
    public static SonarrV5QualityProfileData ToDomain(SonarrApi.QualityProfileResource dto)
    {
        var sizes = new Dictionary<string, QualityItemSize>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in (dto.Items ?? []).Flatten(x => x.Items ?? []))
        {
            if (item.Quality?.Name is { } name && SizeToDomain(item) is { } size)
            {
                sizes.TryAdd(name, size);
            }
        }

        return ProfileToDomain(dto) with
        {
            QualitySizes = sizes,
        };
    }

    private static partial SonarrV5QualityProfileData ProfileToDomain(
        SonarrApi.QualityProfileResource dto
    );

    [MapProperty(
        nameof(SonarrApi.ProfileFormatItemResource.Format),
        nameof(QualityProfileFormatItem.FormatId)
    )]
    private static partial QualityProfileFormatItem FormatItemToDomain(
        SonarrApi.ProfileFormatItemResource dto
    );

    private static QualityProfileItem ItemToDomain(SonarrApi.QualityProfileQualityItemResource dto)
    {
        return new QualityProfileItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Allowed = dto.Allowed,
            Quality = dto.Quality is not null
                ? new QualityProfileItemQuality { Id = dto.Quality.Id, Name = dto.Quality.Name }
                : null,
            Items = (dto.Items ?? []).Select(ItemToDomain).ToList(),
        };
    }

    private static QualityItemSize? SizeToDomain(SonarrApi.QualityProfileQualityItemResource dto)
    {
        if (dto is { MinSize: null, MaxSize: null, PreferredSize: null })
        {
            return null;
        }

        return new QualityItemSize(
            (decimal)(dto.MinSize ?? 0),
            (decimal?)dto.MaxSize,
            (decimal?)dto.PreferredSize
        );
    }
}
