using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.Tag;

public interface ISonarrTagApiService
{
    Task<IList<SonarrTag>> GetTags(IServiceConfiguration config);
    Task<SonarrTag> CreateTag(IServiceConfiguration config, string tag);
}
