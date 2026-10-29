using System.Globalization;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using Recyclarr.TrashGuide.CustomFormat;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.CustomFormat;

internal class RadarrCustomFormatGateway(RadarrApi.ICustomFormatApi api) : ICustomFormatService
{
    private Dictionary<string, RadarrApi.CustomFormatSpecificationSchema>? _specSchemas;

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
        var result = await api.CustomformatPost(await ToDto(cf, ct), ct);
        return RadarrCustomFormatMapper.ToDomain(result);
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
    // keep the service's defaults instead of being omitted.
    private async Task<RadarrApi.CustomFormatResource> ToDto(
        CustomFormatResource cf,
        CancellationToken ct
    )
    {
        _specSchemas ??= (await api.Schema(ct))
            .Where(x => x.Implementation is not null)
            .ToDictionary(x => x.Implementation!, StringComparer.OrdinalIgnoreCase);

        var dto = RadarrCustomFormatMapper.FromDomain(cf);
        dto.Specifications = cf.Specifications.Select(x => SpecToDto(x, _specSchemas)).ToList();
        return dto;
    }

    private static RadarrApi.CustomFormatSpecificationSchema SpecToDto(
        CustomFormatSpecificationData spec,
        Dictionary<string, RadarrApi.CustomFormatSpecificationSchema> schemas
    )
    {
        if (!schemas.TryGetValue(spec.Implementation, out var schema))
        {
            return RadarrCustomFormatMapper.SpecFromDomain(spec);
        }

        var dto = RadarrCustomFormatMapper.CloneSpec(schema);
        // Presets are UI starting points for a new specification, not part of a saved one
        dto.ExtensionData.Remove("presets");
        RadarrCustomFormatMapper.UpdateSpec(spec, dto);

        var fields = dto.Fields?.ToList() ?? [];
        foreach (var field in spec.Fields)
        {
            var target = fields.Find(x =>
                string.Equals(x.Name, field.Name, StringComparison.OrdinalIgnoreCase)
            );

            if (target is null)
            {
                fields.Add(RadarrCustomFormatMapper.FieldFromDomain(field));
                continue;
            }

            RadarrCustomFormatMapper.UpdateField(field, target);
        }

        dto.Fields = fields;
        return dto;
    }

    public async Task DeleteCustomFormat(int customFormatId, CancellationToken ct)
    {
        await api.CustomformatDelete(customFormatId, ct);
    }
}
