namespace Recyclarr.ServarrApi.QualityDefinition;

public interface IQualityDefinitionApiService
{
    Task<IList<ServiceQualityDefinitionItem>> GetQualityDefinition(CancellationToken ct);

    Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(
        IList<ServiceQualityDefinitionItem> newQuality,
        CancellationToken ct
    );

    Task ResetQualityDefinitions(CancellationToken ct);
}
