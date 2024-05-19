namespace Recyclarr.ServarrApi.MediaNaming;

public interface IMediaNamingApiService
{
    Task<MediaNamingDto> GetNaming();
    Task UpdateNaming(MediaNamingDto dto);
}
