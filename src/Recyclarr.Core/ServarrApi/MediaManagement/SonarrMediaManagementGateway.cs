using System.Globalization;
using Recyclarr.Servarr.MediaManagement;
using SonarrApi = Recyclarr.Api.Sonarr;

namespace Recyclarr.ServarrApi.MediaManagement;

internal class SonarrMediaManagementGateway(SonarrApi.IMediaManagementConfigApi api)
    : IMediaManagementService
{
    private SonarrApi.MediaManagementConfigResource? _stashedDto;

    public async Task<MediaManagementData> GetMediaManagement(CancellationToken ct)
    {
        var dto = await api.MediamanagementGet(ct);
        _stashedDto = dto;
        return SonarrMediaManagementMapper.ToDomain(dto);
    }

    public async Task UpdateMediaManagement(MediaManagementData data, CancellationToken ct)
    {
        // non-null: GetMediaManagement always called before UpdateMediaManagement in pipeline
        var original = _stashedDto!;
        SonarrMediaManagementMapper.UpdateDto(data, original);
        await api.MediamanagementPut(
            original.Id!.Value.ToString(CultureInfo.InvariantCulture),
            original,
            ct
        );
    }
}
