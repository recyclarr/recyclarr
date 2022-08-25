using TrashLib.Services.Sonarr.Api.Objects;

namespace TrashLib.Services.Sonarr.Api;

public interface ISonarrApi
{
    Task<IList<SonarrTag>> GetTags();
    Task<SonarrTag> CreateTag(string tag);
    Task<IList<SonarrReleaseProfile>> GetReleaseProfiles();
    Task UpdateReleaseProfile(SonarrReleaseProfile profileToUpdate);
    Task<SonarrReleaseProfile> CreateReleaseProfile(SonarrReleaseProfile newProfile);
    Task DeleteReleaseProfile(int releaseProfileId);
    Task<IReadOnlyCollection<SonarrQualityDefinitionItem>> GetQualityDefinition();

    Task<IList<SonarrQualityDefinitionItem>> UpdateQualityDefinition(
        IReadOnlyCollection<SonarrQualityDefinitionItem> newQuality);
}
