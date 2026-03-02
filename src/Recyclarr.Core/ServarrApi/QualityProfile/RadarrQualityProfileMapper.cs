using Recyclarr.Servarr.QualityProfile;
using Riok.Mapperly.Abstractions;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.QualityProfile;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class RadarrQualityProfileMapper
{
    public static partial QualityProfileData ToDomain(RadarrApi.QualityProfileResource dto);

    public static partial ProfileLanguage ToDomain(RadarrApi.LanguageResource dto);

    [MapProperty(
        nameof(RadarrApi.ProfileFormatItemResource.Format),
        nameof(QualityProfileFormatItem.FormatId)
    )]
    private static partial QualityProfileFormatItem FormatItemToDomain(
        RadarrApi.ProfileFormatItemResource dto
    );

    private static QualityProfileItem ItemToDomain(RadarrApi.QualityProfileQualityItemResource dto)
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

    // Radarr profile has a Language field; map it to the domain ProfileLanguage
    private static ProfileLanguage? LanguageToDomain(RadarrApi.Language? dto)
    {
        return dto is { Id: { } id, Name: { } name }
            ? new ProfileLanguage { Id = id, Name = name }
            : null;
    }
}
