using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.QualitySize.Api;

public interface IQualityDefinitionService
{
    Task<List<ServiceQualityDefinitionItem>> GetQualityDefinition(IServiceConfiguration config);

    Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(
        IServiceConfiguration config,
        IList<ServiceQualityDefinitionItem> newQuality);
}
