using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Pipelines.Tags.Api;

public interface ISonarrTagApiService
{
    Task<IList<SonarrTag>> GetTags(IServiceConfiguration config);
    Task<SonarrTag> CreateTag(IServiceConfiguration config, string tag);
}
