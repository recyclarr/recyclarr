using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api.Objects;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api;

public interface ISonarrReleaseProfileCompatibilityHandler
{
    Task<object> CompatibleReleaseProfileForSending(
        IServiceConfiguration config,
        SonarrReleaseProfile profile);

    SonarrReleaseProfile CompatibleReleaseProfileForReceiving(JObject profile);
}
