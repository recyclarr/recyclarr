using Recyclarr.Servarr.QualitySize;

namespace Recyclarr.ServarrApi.QualityDefinition;

internal class SonarrQualityDefinitionGateway(IQualityDefinitionApiService api)
    : IQualityDefinitionService
{
    private readonly Dictionary<int, ServiceQualityDefinitionItem> _stashedDtos = [];

    public async Task<IReadOnlyList<QualityDefinitionItem>> GetQualityDefinitions(
        CancellationToken ct
    )
    {
        var dtos = await api.GetQualityDefinition(ct);
        foreach (var dto in dtos)
        {
            _stashedDtos[dto.Id] = dto;
        }

        return dtos.Select(ToDomain).ToList();
    }

    public async Task UpdateQualityDefinitions(
        IReadOnlyList<QualityDefinitionItem> items,
        CancellationToken ct
    )
    {
        var apiItems = items.Select(FromDomain).ToList();
        await api.UpdateQualityDefinition(apiItems, ct);
    }

    private static QualityDefinitionItem ToDomain(ServiceQualityDefinitionItem dto)
    {
        return new QualityDefinitionItem
        {
            Id = dto.Id,
            QualityName = dto.Quality?.Name ?? "",
            MinSize = dto.MinSize,
            MaxSize = dto.MaxSize,
            PreferredSize = dto.PreferredSize,
        };
    }

    // Merges domain changes onto the stashed DTO for round-trip safety
    private ServiceQualityDefinitionItem FromDomain(QualityDefinitionItem domain)
    {
        var original = _stashedDtos[domain.Id];
        return original with
        {
            MinSize = domain.MinSize,
            MaxSize = domain.MaxSize,
            PreferredSize = domain.PreferredSize,
        };
    }
}
