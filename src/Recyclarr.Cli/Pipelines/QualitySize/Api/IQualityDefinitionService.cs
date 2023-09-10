using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Pipelines.QualitySize.Api;

public interface IQualityDefinitionService
{
    Task<IList<ServiceQualityDefinitionItem>> GetQualityDefinition(IServiceConfiguration config);

    Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(
        IServiceConfiguration config,
        IList<ServiceQualityDefinitionItem> newQuality);
}
