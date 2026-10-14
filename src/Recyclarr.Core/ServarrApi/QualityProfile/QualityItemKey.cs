using Recyclarr.Servarr.QualityProfile;
using RadarrApi = Recyclarr.Api.Radarr;
using SonarrApi = Recyclarr.Api.Sonarr.V3;
using SonarrV5Api = Recyclarr.Api.Sonarr.V5;

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

    public static QualityItemKey From(SonarrV5Api.QualityProfileQualityItemResource dto)
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
