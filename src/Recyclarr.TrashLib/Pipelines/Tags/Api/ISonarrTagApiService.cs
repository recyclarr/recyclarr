using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Pipelines.Tags.Api;

public interface ISonarrTagApiService
{
    Task<IList<SonarrTag>> GetTags(IServiceConfiguration config);
    Task<SonarrTag> CreateTag(IServiceConfiguration config, string tag);
}
