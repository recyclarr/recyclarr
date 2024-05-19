namespace Recyclarr.ServarrApi.QualityDefinition;

public interface IQualityDefinitionApiService
{
    Task<IList<ServiceQualityDefinitionItem>> GetQualityDefinition();
    Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(IList<ServiceQualityDefinitionItem> newQuality);
}
