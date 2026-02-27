namespace Recyclarr.Servarr.QualitySize;

public interface IQualityDefinitionService
{
    Task<IReadOnlyList<QualityDefinitionItem>> GetQualityDefinitions(CancellationToken ct);
    Task UpdateQualityDefinitions(IReadOnlyList<QualityDefinitionItem> items, CancellationToken ct);
}
