using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Dto;

namespace Recyclarr.ServarrApi.Services;

public interface IQualityDefinitionService
{
    Task<IList<ServiceQualityDefinitionItem>> GetQualityDefinition(IServiceConfiguration config);

    Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(
        IServiceConfiguration config,
        IList<ServiceQualityDefinitionItem> newQuality);
}
