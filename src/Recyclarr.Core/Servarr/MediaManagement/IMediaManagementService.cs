namespace Recyclarr.Servarr.MediaManagement;

public interface IMediaManagementService
{
    Task<MediaManagementData> GetMediaManagement(CancellationToken ct);
    Task UpdateMediaManagement(MediaManagementData data, CancellationToken ct);
}
