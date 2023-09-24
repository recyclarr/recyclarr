using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.QualityDefinition;

public interface IQualityDefinitionApiService
{
    Task<IList<ServiceQualityDefinitionItem>> GetQualityDefinition(IServiceConfiguration config);

    Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(
        IServiceConfiguration config,
        IList<ServiceQualityDefinitionItem> newQuality);
}
