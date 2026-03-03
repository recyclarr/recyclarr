using System.Globalization;
using Recyclarr.Servarr.MediaNaming;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.MediaNaming;

internal class RadarrNamingGateway(RadarrApi.INamingConfigApi api) : IRadarrNamingService
{
    private RadarrApi.NamingConfigResource? _stashedDto;

    public async Task<RadarrNamingData> GetNaming(CancellationToken ct)
    {
        var dto = await api.NamingGet(ct);
        _stashedDto = dto;
        return RadarrNamingMapper.ToDomain(dto);
    }

    public async Task UpdateNaming(RadarrNamingData data, CancellationToken ct)
    {
        // non-null: GetNaming always called before UpdateNaming in pipeline
        var original = _stashedDto!;
        RadarrNamingMapper.UpdateDto(data, original);
        await api.NamingPut(
            original.Id!.Value.ToString(CultureInfo.InvariantCulture),
            original,
            ct
        );
    }
}
