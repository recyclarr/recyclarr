using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trash.Sonarr.Api.Objects;

namespace Trash.Sonarr.Api
{
    public interface ISonarrApi
    {
        Task<Version> GetVersion();
        Task<List<SonarrTag>> GetTags();
        Task<SonarrTag> CreateTag(string tag);
        Task<List<SonarrReleaseProfile>> GetReleaseProfiles();
        Task UpdateReleaseProfile(SonarrReleaseProfile profileToUpdate);
        Task<SonarrReleaseProfile> CreateReleaseProfile(SonarrReleaseProfile newProfile);
        Task<List<SonarrQualityDefinitionItem>> GetQualityDefinition();
        Task<List<SonarrQualityDefinitionItem>> UpdateQualityDefinition(List<SonarrQualityDefinitionItem> newQuality);
    }
}
