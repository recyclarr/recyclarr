using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.ServarrApi.MediaNaming;

internal class RadarrNamingGateway(IRadarrMediaNamingApiService api) : IRadarrNamingService
{
    private RadarrMediaNamingDto? _stashedDto;

    public async Task<RadarrNamingData> GetNaming(CancellationToken ct)
    {
        var dto = await api.GetNaming(ct);
        _stashedDto = dto;
        return ToDomain(dto);
    }

    public async Task UpdateNaming(RadarrNamingData data, CancellationToken ct)
    {
        var dto = FromDomain(data);
        await api.UpdateNaming(dto, ct);
    }

    private static RadarrNamingData ToDomain(RadarrMediaNamingDto dto)
    {
        return new RadarrNamingData
        {
            RenameMovies = dto.RenameMovies,
            StandardMovieFormat = dto.StandardMovieFormat,
            MovieFolderFormat = dto.MovieFolderFormat,
        };
    }

    // Merges domain changes onto the stashed DTO for round-trip safety
    private RadarrMediaNamingDto FromDomain(RadarrNamingData data)
    {
        // non-null: GetNaming always called before UpdateNaming in pipeline
        var original = _stashedDto!;
        return original with
        {
            RenameMovies = data.RenameMovies,
            StandardMovieFormat = data.StandardMovieFormat,
            MovieFolderFormat = data.MovieFolderFormat,
        };
    }
}
