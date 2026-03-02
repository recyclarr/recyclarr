using Recyclarr.Servarr.QualityProfile;
using RadarrApi = Recyclarr.Api.Radarr;
using SonarrApi = Recyclarr.Api.Sonarr;

namespace Recyclarr.ServarrApi.QualityProfile;

// Discriminated key for quality item lookup: distinguishes qualities (by Quality.Id) from groups (by group Id)
internal readonly record struct QualityItemKey(bool IsGroup, int? Id)
{
    public static QualityItemKey From(SonarrApi.QualityProfileQualityItemResource dto)
    {
        return dto.Quality is null
            ? new QualityItemKey(true, dto.Id)
            : new QualityItemKey(false, dto.Quality.Id);
    }

    public static QualityItemKey From(RadarrApi.QualityProfileQualityItemResource dto)
    {
        return dto.Quality is null
            ? new QualityItemKey(true, dto.Id)
            : new QualityItemKey(false, dto.Quality.Id);
    }

    public static QualityItemKey From(QualityProfileItem domain)
    {
        return domain.Quality is null
            ? new QualityItemKey(true, domain.Id)
            : new QualityItemKey(false, domain.Quality.Id);
    }
}
