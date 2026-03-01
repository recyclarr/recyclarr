using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.ServarrApi.MediaNaming;

internal class SonarrNamingGateway(ISonarrMediaNamingApiService api) : ISonarrNamingService
{
    private ServiceSonarrNamingData? _stashedDto;

    public async Task<SonarrNamingData> GetNaming(CancellationToken ct)
    {
        var dto = await api.GetNaming(ct);
        _stashedDto = dto;
        return ToDomain(dto);
    }

    public async Task UpdateNaming(SonarrNamingData data, CancellationToken ct)
    {
        var dto = FromDomain(data);
        await api.UpdateNaming(dto, ct);
    }

    private static SonarrNamingData ToDomain(ServiceSonarrNamingData dto)
    {
        return new SonarrNamingData
        {
            RenameEpisodes = dto.RenameEpisodes,
            SeriesFolderFormat = dto.SeriesFolderFormat,
            SeasonFolderFormat = dto.SeasonFolderFormat,
            StandardEpisodeFormat = dto.StandardEpisodeFormat,
            DailyEpisodeFormat = dto.DailyEpisodeFormat,
            AnimeEpisodeFormat = dto.AnimeEpisodeFormat,
        };
    }

    // Merges domain changes onto the stashed DTO for round-trip safety
    private ServiceSonarrNamingData FromDomain(SonarrNamingData data)
    {
        // non-null: GetNaming always called before UpdateNaming in pipeline
        var original = _stashedDto!;
        return original with
        {
            RenameEpisodes = data.RenameEpisodes,
            SeriesFolderFormat = data.SeriesFolderFormat,
            SeasonFolderFormat = data.SeasonFolderFormat,
            StandardEpisodeFormat = data.StandardEpisodeFormat,
            DailyEpisodeFormat = data.DailyEpisodeFormat,
            AnimeEpisodeFormat = data.AnimeEpisodeFormat,
        };
    }
}
