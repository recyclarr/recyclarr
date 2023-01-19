using Recyclarr.TrashLib.Services.Sonarr.Api.Objects;

namespace Recyclarr.TrashLib.Services.Sonarr.Api;

public interface ISonarrTagApiService
{
    Task<IList<SonarrTag>> GetTags();
    Task<SonarrTag> CreateTag(string tag);
}
