using Recyclarr.Servarr.MediaManagement;

namespace Recyclarr.ServarrApi.MediaManagement;

internal class SonarrMediaManagementGateway(IMediaManagementApiService api)
    : IMediaManagementService
{
    private MediaManagementDto? _stashedDto;

    public async Task<MediaManagementData> GetMediaManagement(CancellationToken ct)
    {
        var dto = await api.GetMediaManagement(ct);
        _stashedDto = dto;
        return ToDomain(dto);
    }

    public async Task UpdateMediaManagement(MediaManagementData data, CancellationToken ct)
    {
        var dto = FromDomain(data);
        await api.UpdateMediaManagement(dto, ct);
    }

    private static MediaManagementData ToDomain(MediaManagementDto dto)
    {
        return new MediaManagementData
        {
            Id = dto.Id,
            PropersAndRepacks = dto.DownloadPropersAndRepacks,
        };
    }

    // Merges domain changes onto the stashed DTO for round-trip safety
    private MediaManagementDto FromDomain(MediaManagementData data)
    {
        // non-null: GetMediaManagement always called before UpdateMediaManagement in pipeline
        var original = _stashedDto!;
        return original with { DownloadPropersAndRepacks = data.PropersAndRepacks };
    }
}
