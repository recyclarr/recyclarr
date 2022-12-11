namespace TrashLib.Services.QualitySize.Api;

public interface IQualityDefinitionService
{
    Task<List<ServiceQualityDefinitionItem>> GetQualityDefinition();
    Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(IList<ServiceQualityDefinitionItem> newQuality);
}
