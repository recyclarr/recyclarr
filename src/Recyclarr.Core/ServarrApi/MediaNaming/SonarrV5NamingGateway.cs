using System.Globalization;
using Recyclarr.Servarr.MediaNaming;
using SonarrApi = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.ServarrApi.MediaNaming;

internal class SonarrV5NamingGateway(SonarrApi.INamingSettingsApi api) : ISonarrNamingService
{
    private SonarrApi.NamingSettingsResource? _stashedDto;

    public async Task<SonarrNamingData> GetNaming(CancellationToken ct)
    {
        var dto = await api.NamingGet(ct);
        _stashedDto = dto;
        return SonarrV5NamingMapper.ToDomain(dto);
    }

    public async Task UpdateNaming(SonarrNamingData data, CancellationToken ct)
    {
        // non-null: GetNaming always called before UpdateNaming in pipeline
        var original = _stashedDto!;
        SonarrV5NamingMapper.UpdateDto(data, original);
        await api.NamingPut(
            original.Id!.Value.ToString(CultureInfo.InvariantCulture),
            original,
            ct
        );
    }
}
