namespace Recyclarr.ServarrApi.MediaManagement;

public interface IMediaManagementApiService
{
    Task<MediaManagementDto> GetMediaManagement(CancellationToken ct);
    Task UpdateMediaManagement(MediaManagementDto dto, CancellationToken ct);
}
