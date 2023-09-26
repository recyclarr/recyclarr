using Flurl.Http;
using Recyclarr.Common;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Http;

namespace Recyclarr.ServarrApi.MediaNaming;

public class MediaNamingApiService : IMediaNamingApiService
{
    private readonly IServiceRequestBuilder _service;

    public MediaNamingApiService(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task<MediaNamingDto> GetNaming(IServiceConfiguration config)
    {
        var response = await _service.Request(config, "config", "naming")
            .GetAsync();

        return config.ServiceType switch
        {
            SupportedServices.Radarr => await response.GetJsonAsync<RadarrMediaNamingDto>(),
            SupportedServices.Sonarr => await response.GetJsonAsync<SonarrMediaNamingDto>(),
            _ => throw new ArgumentException("Configuration type unsupported in GetNaming() API")
        };
    }

    public async Task UpdateNaming(IServiceConfiguration config, MediaNamingDto dto)
    {
        await _service.Request(config, "config", "naming")
            .PutJsonAsync(dto);
    }
}
