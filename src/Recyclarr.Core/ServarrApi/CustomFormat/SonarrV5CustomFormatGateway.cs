using System.Globalization;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using SonarrApi = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.ServarrApi.CustomFormat;

internal class SonarrV5CustomFormatGateway(SonarrApi.ICustomFormatApi api) : ICustomFormatService
{
    private Dictionary<string, SonarrApi.CustomFormatSpecificationSchema>? _specSchemas;

    public async Task<IReadOnlyList<CustomFormatResource>> GetCustomFormats(CancellationToken ct)
    {
        var result = await api.CustomformatGet(ct);
        return result.Select(SonarrV5CustomFormatMapper.ToDomain).ToList();
    }

    public async Task<CustomFormatResource?> CreateCustomFormat(
        CustomFormatResource cf,
        CancellationToken ct
    )
    {
        var result = await api.CustomformatPost(await ToDto(cf, ct), ct);
        return SonarrV5CustomFormatMapper.ToDomain(result);
    }

    public async Task UpdateCustomFormat(CustomFormatResource cf, CancellationToken ct)
    {
        await api.CustomformatPut(
            cf.Id.ToString(CultureInfo.InvariantCulture),
            await ToDto(cf, ct),
            ct
        );
    }

    // The v5 API rejects specifications without `implementationName` and `infoLink`. Guide data
    // does not carry them, so they are filled from the service's specification schema.
    private async Task<SonarrApi.CustomFormatResource> ToDto(
        CustomFormatResource cf,
        CancellationToken ct
    )
    {
        _specSchemas ??= (await api.Schema(ct))
            .Where(x => x.Implementation is not null)
            .ToDictionary(x => x.Implementation!, StringComparer.OrdinalIgnoreCase);

        var dto = SonarrV5CustomFormatMapper.FromDomain(cf);
        foreach (var spec in dto.Specifications ?? [])
        {
            if (spec.Implementation is null)
            {
                continue;
            }

            if (_specSchemas.TryGetValue(spec.Implementation, out var schema))
            {
                spec.ImplementationName ??= schema.ImplementationName;
                spec.InfoLink ??= schema.InfoLink;
            }
        }

        return dto;
    }

    public async Task DeleteCustomFormat(int customFormatId, CancellationToken ct)
    {
        await api.CustomformatDelete(customFormatId, ct);
    }
}
