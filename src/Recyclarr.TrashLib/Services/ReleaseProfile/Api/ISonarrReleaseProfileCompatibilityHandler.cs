using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.ReleaseProfile.Api.Objects;

namespace Recyclarr.TrashLib.Services.ReleaseProfile.Api;

public interface ISonarrReleaseProfileCompatibilityHandler
{
    Task<object> CompatibleReleaseProfileForSending(IServiceConfiguration config,
        SonarrReleaseProfile profile);

    SonarrReleaseProfile CompatibleReleaseProfileForReceiving(JObject profile);
}
