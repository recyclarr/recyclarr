using TrashLib.Services.Sonarr.Api.Objects;

namespace TrashLib.Services.Sonarr.Api;

public interface ISonarrApi
{
    Task<IList<SonarrTag>> GetTags();
    Task<SonarrTag> CreateTag(string tag);
    Task<IReadOnlyCollection<SonarrQualityDefinitionItem>> GetQualityDefinition();

    Task<IList<SonarrQualityDefinitionItem>> UpdateQualityDefinition(
        IReadOnlyCollection<SonarrQualityDefinitionItem> newQuality);

    Task<Version> GetVersion();
}
