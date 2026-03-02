using Recyclarr.Servarr.QualityProfile;
using Riok.Mapperly.Abstractions;
using SonarrApi = Recyclarr.Api.Sonarr;

namespace Recyclarr.ServarrApi.QualityProfile;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class SonarrQualityProfileMapper
{
    public static partial QualityProfileData ToDomain(SonarrApi.QualityProfileResource dto);

    public static partial ProfileLanguage ToDomain(SonarrApi.LanguageResource dto);

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
}
