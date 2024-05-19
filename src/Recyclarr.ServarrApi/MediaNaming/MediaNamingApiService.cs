using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.ServarrApi.MediaNaming;

public class MediaNamingApiService(IServarrRequestBuilder service, IServiceConfiguration config)
    : IMediaNamingApiService
{
    public async Task<MediaNamingDto> GetNaming()
    {
        var response = await service.Request("config", "naming").GetAsync();
        return config.ServiceType switch
        {
            SupportedServices.Radarr => await response.GetJsonAsync<RadarrMediaNamingDto>(),
            SupportedServices.Sonarr => await response.GetJsonAsync<SonarrMediaNamingDto>(),
            _ => throw new ArgumentException("Configuration type unsupported in GetNaming() API")
        };
    }

    public async Task UpdateNaming(MediaNamingDto dto)
    {
        await service.Request("config", "naming")
            .PutJsonAsync(dto);
    }
}
