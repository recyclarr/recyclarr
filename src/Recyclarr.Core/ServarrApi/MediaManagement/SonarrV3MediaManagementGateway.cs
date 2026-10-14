using System.Globalization;
using Recyclarr.Servarr.MediaManagement;
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.ServarrApi.MediaManagement;

internal class SonarrV3MediaManagementGateway(SonarrApi.IMediaManagementConfigApi api)
    : IMediaManagementService
{
    private SonarrApi.MediaManagementConfigResource? _stashedDto;

    public async Task<MediaManagementData> GetMediaManagement(CancellationToken ct)
    {
        var dto = await api.MediamanagementGet(ct);
        _stashedDto = dto;
        return SonarrV3MediaManagementMapper.ToDomain(dto);
    }

    public async Task UpdateMediaManagement(MediaManagementData data, CancellationToken ct)
    {
        // non-null: GetMediaManagement always called before UpdateMediaManagement in pipeline
        var original = _stashedDto!;
        SonarrV3MediaManagementMapper.UpdateDto(data, original);
        await api.MediamanagementPut(
            original.Id!.Value.ToString(CultureInfo.InvariantCulture),
            original,
            ct
        );
    }
}
