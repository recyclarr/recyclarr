namespace Recyclarr.ServarrApi.MediaManagement;

public interface IMediaManagementApiService
{
    Task<ServiceMediaManagementData> GetMediaManagement(CancellationToken ct);
    Task UpdateMediaManagement(ServiceMediaManagementData dto, CancellationToken ct);
}
