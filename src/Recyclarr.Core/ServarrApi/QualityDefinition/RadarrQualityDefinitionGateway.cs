using Recyclarr.Servarr.QualitySize;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.QualityDefinition;

internal class RadarrQualityDefinitionGateway(RadarrApi.IQualityDefinitionApi api)
    : IQualityDefinitionService
{
    private readonly Dictionary<int, RadarrApi.QualityDefinitionResource> _stashedDtos = [];

    public async Task<IReadOnlyList<QualityDefinitionItem>> GetQualityDefinitions(
        CancellationToken ct
    )
    {
        var dtos = await api.QualitydefinitionGet(ct);
        foreach (var dto in dtos)
        {
            _stashedDtos[dto.Id] = dto;
        }

        return dtos.Select(RadarrQualityDefinitionMapper.ToDomain).ToList();
    }

    public async Task UpdateQualityDefinitions(
        IReadOnlyList<QualityDefinitionItem> items,
        CancellationToken ct
    )
    {
        // Merge domain changes onto stashed DTOs, then send batch update
        var apiItems = items.Select(ApplyToStashed).ToList();
        await api.Update(apiItems, ct);
    }

    private RadarrApi.QualityDefinitionResource ApplyToStashed(QualityDefinitionItem domain)
    {
        var original = _stashedDtos[domain.Id];
        RadarrQualityDefinitionMapper.UpdateDto(domain, original);
        return original;
    }
}
