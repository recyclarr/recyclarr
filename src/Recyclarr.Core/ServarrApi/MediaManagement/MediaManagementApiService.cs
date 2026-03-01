using Flurl.Http;

namespace Recyclarr.ServarrApi.MediaManagement;

internal class MediaManagementApiService(IServarrRequestBuilder service)
    : IMediaManagementApiService
{
    private IFlurlRequest Request(params object[] path)
    {
        return service.Request(["config", "mediamanagement", .. path]);
    }

    public async Task<ServiceMediaManagementData> GetMediaManagement(CancellationToken ct)
    {
        return await Request().GetJsonAsync<ServiceMediaManagementData>(cancellationToken: ct);
    }

    public async Task UpdateMediaManagement(ServiceMediaManagementData dto, CancellationToken ct)
    {
        await Request(dto.Id).PutJsonAsync(dto, cancellationToken: ct);
    }
}
