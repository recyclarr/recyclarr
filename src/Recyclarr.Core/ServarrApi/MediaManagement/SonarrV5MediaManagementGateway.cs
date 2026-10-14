using System.Globalization;
using Recyclarr.Servarr.MediaManagement;
using SonarrApi = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.ServarrApi.MediaManagement;

internal class SonarrV5MediaManagementGateway(SonarrApi.IMediaManagementSettingsApi api)
    : IMediaManagementService
{
    private SonarrApi.MediaManagementSettingsResource? _stashedDto;

    public async Task<MediaManagementData> GetMediaManagement(CancellationToken ct)
    {
        var dto = await api.MediamanagementGet(ct);
        _stashedDto = dto;
        return SonarrV5MediaManagementMapper.ToDomain(dto);
    }

    public async Task UpdateMediaManagement(MediaManagementData data, CancellationToken ct)
    {
        // non-null: GetMediaManagement always called before UpdateMediaManagement in pipeline
        var original = _stashedDto!;
        SonarrV5MediaManagementMapper.UpdateDto(data, original);
        await api.MediamanagementPut(
            original.Id!.Value.ToString(CultureInfo.InvariantCulture),
            original,
            ct
        );
    }
}
