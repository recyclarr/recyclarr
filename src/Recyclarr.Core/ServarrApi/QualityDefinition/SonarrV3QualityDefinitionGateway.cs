using Recyclarr.Servarr.QualitySize;
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.ServarrApi.QualityDefinition;

internal class SonarrV3QualityDefinitionGateway(SonarrApi.IQualityDefinitionApi api)
    : IQualityDefinitionService
{
    private readonly Dictionary<int, SonarrApi.QualityDefinitionResource> _stashedDtos = [];

    public async Task<IReadOnlyList<QualityDefinitionItem>> GetQualityDefinitions(
        CancellationToken ct
    )
    {
        var dtos = await api.QualitydefinitionGet(ct);
        foreach (var dto in dtos)
        {
            if (dto.Id is { } id)
            {
                _stashedDtos[id] = dto;
            }
        }

        return dtos.Select(SonarrV3QualityDefinitionMapper.ToDomain).ToList();
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

    private SonarrApi.QualityDefinitionResource ApplyToStashed(QualityDefinitionItem domain)
    {
        var original = _stashedDtos[domain.Id];
        SonarrV3QualityDefinitionMapper.UpdateDto(domain, original);
        return original;
    }
}
