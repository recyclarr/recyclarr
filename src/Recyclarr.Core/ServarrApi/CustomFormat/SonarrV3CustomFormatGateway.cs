using System.Globalization;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.ServarrApi.CustomFormat;

internal class SonarrV3CustomFormatGateway(SonarrApi.ICustomFormatApi api) : ICustomFormatService
{
    public async Task<IReadOnlyList<CustomFormatResource>> GetCustomFormats(CancellationToken ct)
    {
        var result = await api.CustomformatGet(ct);
        return result.Select(SonarrV3CustomFormatMapper.ToDomain).ToList();
    }

    public async Task<CustomFormatResource?> CreateCustomFormat(
        CustomFormatResource cf,
        CancellationToken ct
    )
    {
        var result = await api.CustomformatPost(SonarrV3CustomFormatMapper.FromDomain(cf), ct);
        return SonarrV3CustomFormatMapper.ToDomain(result);
    }

    public async Task UpdateCustomFormat(CustomFormatResource cf, CancellationToken ct)
    {
        await api.CustomformatPut(
            cf.Id.ToString(CultureInfo.InvariantCulture),
            SonarrV3CustomFormatMapper.FromDomain(cf),
            ct
        );
    }

    public async Task DeleteCustomFormat(int customFormatId, CancellationToken ct)
    {
        await api.CustomformatDelete(customFormatId, ct);
    }
}
