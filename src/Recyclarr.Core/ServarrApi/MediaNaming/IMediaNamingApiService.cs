namespace Recyclarr.ServarrApi.MediaNaming;

public interface IMediaNamingApiService
{
    Task<MediaNamingDto> GetNaming(CancellationToken ct);
    Task UpdateNaming(MediaNamingDto dto, CancellationToken ct);
}
