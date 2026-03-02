using System.Globalization;
using Recyclarr.Servarr.MediaManagement;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.MediaManagement;

internal class RadarrMediaManagementGateway(RadarrApi.IMediaManagementConfigApi api)
    : IMediaManagementService
{
    private RadarrApi.MediaManagementConfigResource? _stashedDto;

    public async Task<MediaManagementData> GetMediaManagement(CancellationToken ct)
    {
        var dto = await api.MediamanagementGet(ct);
        _stashedDto = dto;
        return RadarrMediaManagementMapper.ToDomain(dto);
    }

    public async Task UpdateMediaManagement(MediaManagementData data, CancellationToken ct)
    {
        // non-null: GetMediaManagement always called before UpdateMediaManagement in pipeline
        var original = _stashedDto!;
        RadarrMediaManagementMapper.UpdateDto(data, original);
        await api.MediamanagementPut(
            original.Id!.Value.ToString(CultureInfo.InvariantCulture),
            original,
            ct
        );
    }
}
