using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Dto;

namespace Recyclarr.ServarrApi.Services;

public interface ISonarrTagApiService
{
    Task<IList<SonarrTag>> GetTags(IServiceConfiguration config);
    Task<SonarrTag> CreateTag(IServiceConfiguration config, string tag);
}
