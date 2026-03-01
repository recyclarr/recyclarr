using System.Globalization;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.CustomFormat;

internal class RadarrCustomFormatGateway(RadarrApi.ICustomFormatApi api) : ICustomFormatService
{
    public async Task<IReadOnlyList<CustomFormatResource>> GetCustomFormats(CancellationToken ct)
    {
        var result = await api.CustomformatGet(ct);
        return result.Select(RadarrCustomFormatMapper.ToDomain).ToList();
    }

    public async Task<CustomFormatResource?> CreateCustomFormat(
        CustomFormatResource cf,
        CancellationToken ct
    )
    {
        var result = await api.CustomformatPost(RadarrCustomFormatMapper.FromDomain(cf), ct);
        return RadarrCustomFormatMapper.ToDomain(result);
    }

    public async Task UpdateCustomFormat(CustomFormatResource cf, CancellationToken ct)
    {
        await api.CustomformatPut(
            cf.Id.ToString(CultureInfo.InvariantCulture),
            RadarrCustomFormatMapper.FromDomain(cf),
            ct
        );
    }

    public async Task DeleteCustomFormat(int customFormatId, CancellationToken ct)
    {
        await api.CustomformatDelete(customFormatId, ct);
    }
}
