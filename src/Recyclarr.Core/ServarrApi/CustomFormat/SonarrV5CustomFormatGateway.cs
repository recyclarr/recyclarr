using System.Globalization;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using Recyclarr.TrashGuide.CustomFormat;
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

    // Each specification starts from the service's schema template for its implementation, so
    // members the guide does not carry (display metadata, fields added by newer service versions)
    // keep the service's defaults instead of being omitted. The v5 API rejects specifications
    // that omit members it requires, such as `implementationName` and `infoLink`.
    private async Task<SonarrApi.CustomFormatResource> ToDto(
        CustomFormatResource cf,
        CancellationToken ct
    )
    {
        _specSchemas ??= (await api.Schema(ct))
            .Where(x => x.Implementation is not null)
            .ToDictionary(x => x.Implementation!, StringComparer.OrdinalIgnoreCase);

        var dto = SonarrV5CustomFormatMapper.FromDomain(cf);
        dto.Specifications = cf.Specifications.Select(x => SpecToDto(x, _specSchemas)).ToList();
        return dto;
    }

    private static SonarrApi.CustomFormatSpecificationSchema SpecToDto(
        CustomFormatSpecificationData spec,
        Dictionary<string, SonarrApi.CustomFormatSpecificationSchema> schemas
    )
    {
        if (!schemas.TryGetValue(spec.Implementation, out var schema))
        {
            return SonarrV5CustomFormatMapper.SpecFromDomain(spec);
        }

        var dto = SonarrV5CustomFormatMapper.CloneSpec(schema);
        SonarrV5CustomFormatMapper.UpdateSpec(spec, dto);

        var fields = dto.Fields?.ToList() ?? [];
        foreach (var field in spec.Fields)
        {
            var target = fields.Find(x =>
                string.Equals(x.Name, field.Name, StringComparison.OrdinalIgnoreCase)
            );

            if (target is null)
            {
                fields.Add(SonarrV5CustomFormatMapper.FieldFromDomain(field));
                continue;
            }

            SonarrV5CustomFormatMapper.UpdateField(field, target);
        }

        dto.Fields = fields;
        return dto;
    }

    public async Task DeleteCustomFormat(int customFormatId, CancellationToken ct)
    {
        await api.CustomformatDelete(customFormatId, ct);
    }
}
