using System.Globalization;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using SonarrApi = Recyclarr.Api.Sonarr;

namespace Recyclarr.ServarrApi.CustomFormat;

internal class SonarrCustomFormatGateway(SonarrApi.ICustomFormatApi api) : ICustomFormatService
{
    public async Task<IReadOnlyList<CustomFormatResource>> GetCustomFormats(CancellationToken ct)
    {
        var result = await api.CustomformatGet(ct);
        return result.Select(SonarrCustomFormatMapper.ToDomain).ToList();
    }

    public async Task<CustomFormatResource?> CreateCustomFormat(
        CustomFormatResource cf,
        CancellationToken ct
    )
    {
        var result = await api.CustomformatPost(SonarrCustomFormatMapper.FromDomain(cf), ct);
        return SonarrCustomFormatMapper.ToDomain(result);
    }

    public async Task UpdateCustomFormat(CustomFormatResource cf, CancellationToken ct)
    {
        await api.CustomformatPut(
            cf.Id.ToString(CultureInfo.InvariantCulture),
            SonarrCustomFormatMapper.FromDomain(cf),
            ct
        );
    }

    public async Task DeleteCustomFormat(int customFormatId, CancellationToken ct)
    {
        await api.CustomformatDelete(customFormatId, ct);
    }
}
