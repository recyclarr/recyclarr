using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.Sonarr.Api.Objects;

namespace Recyclarr.TrashLib.Services.Sonarr.Api;

public interface ISonarrTagApiService
{
    Task<IList<SonarrTag>> GetTags(IServiceConfiguration config);
    Task<SonarrTag> CreateTag(IServiceConfiguration config, string tag);
}
