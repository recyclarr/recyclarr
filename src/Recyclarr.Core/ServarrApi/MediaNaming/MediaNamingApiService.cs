using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.ServarrApi.MediaNaming;

public class MediaNamingApiService(IServarrRequestBuilder service, IServiceConfiguration config)
    : IMediaNamingApiService
{
    private IFlurlRequest Request(params object[] path)
    {
        return service.Request(["config", "naming", ..path]);
    }

    public async Task<MediaNamingDto> GetNaming(CancellationToken ct)
    {
        var response = await Request()
            .GetAsync(cancellationToken: ct);

        return config.ServiceType switch
        {
            SupportedServices.Radarr => await response.GetJsonAsync<RadarrMediaNamingDto>(),
            SupportedServices.Sonarr => await response.GetJsonAsync<SonarrMediaNamingDto>(),
            _ => throw new ArgumentException("Configuration type unsupported in GetNaming() API")
        };
    }

    public async Task UpdateNaming(MediaNamingDto dto, CancellationToken ct)
    {
        await Request()
            .PutJsonAsync(dto, cancellationToken: ct);
    }
}
