using System.Globalization;
using Recyclarr.Servarr.MediaNaming;
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.ServarrApi.MediaNaming;

internal class SonarrV3NamingGateway(SonarrApi.INamingConfigApi api) : ISonarrNamingService
{
    private SonarrApi.NamingConfigResource? _stashedDto;

    public async Task<SonarrNamingData> GetNaming(CancellationToken ct)
    {
        var dto = await api.NamingGet(ct);
        _stashedDto = dto;
        return SonarrV3NamingMapper.ToDomain(dto);
    }

    public async Task UpdateNaming(SonarrNamingData data, CancellationToken ct)
    {
        // non-null: GetNaming always called before UpdateNaming in pipeline
        var original = _stashedDto!;
        SonarrV3NamingMapper.UpdateDto(data, original);
        await api.NamingPut(
            original.Id!.Value.ToString(CultureInfo.InvariantCulture),
            original,
            ct
        );
    }
}
