using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trash.Sonarr.Api.Objects;

namespace Trash.Sonarr.Api
{
    public interface ISonarrApi
    {
        Task<Version> GetVersion();
        Task<IList<SonarrTag>> GetTags();
        Task<SonarrTag> CreateTag(string tag);
        Task<IList<SonarrReleaseProfile>> GetReleaseProfiles();
        Task UpdateReleaseProfile(SonarrReleaseProfile profileToUpdate);
        Task<SonarrReleaseProfile> CreateReleaseProfile(SonarrReleaseProfile newProfile);
        Task<IReadOnlyCollection<SonarrQualityDefinitionItem>> GetQualityDefinition();

        Task<IList<SonarrQualityDefinitionItem>> UpdateQualityDefinition(
            IReadOnlyCollection<SonarrQualityDefinitionItem> newQuality);
    }
}
