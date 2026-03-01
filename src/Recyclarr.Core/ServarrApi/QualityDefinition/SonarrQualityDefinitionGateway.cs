using Recyclarr.Servarr.QualitySize;
using SonarrApi = Recyclarr.Api.Sonarr;

namespace Recyclarr.ServarrApi.QualityDefinition;

internal class SonarrQualityDefinitionGateway(SonarrApi.IQualityDefinitionApi api)
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
            _stashedDtos[dto.Id] = dto;
        }

        return dtos.Select(SonarrQualityDefinitionMapper.ToDomain).ToList();
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
        SonarrQualityDefinitionMapper.UpdateDto(domain, original);
        return original;
    }
}
