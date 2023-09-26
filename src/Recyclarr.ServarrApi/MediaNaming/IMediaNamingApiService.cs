using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.MediaNaming;

public interface IMediaNamingApiService
{
    Task<MediaNamingDto> GetNaming(IServiceConfiguration config);
    Task UpdateNaming(IServiceConfiguration config, MediaNamingDto dto);
}
