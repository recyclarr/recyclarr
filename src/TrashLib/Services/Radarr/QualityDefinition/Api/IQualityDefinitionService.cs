using TrashLib.Services.Radarr.QualityDefinition.Api.Objects;

namespace TrashLib.Services.Radarr.QualityDefinition.Api;

public interface IQualityDefinitionService
{
    Task<List<RadarrQualityDefinitionItem>> GetQualityDefinition();
    Task<IList<RadarrQualityDefinitionItem>> UpdateQualityDefinition(IList<RadarrQualityDefinitionItem> newQuality);
}
