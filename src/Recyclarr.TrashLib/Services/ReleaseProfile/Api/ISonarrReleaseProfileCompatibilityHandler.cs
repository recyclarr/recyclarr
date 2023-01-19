using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Services.ReleaseProfile.Api.Objects;

namespace Recyclarr.TrashLib.Services.ReleaseProfile.Api;

public interface ISonarrReleaseProfileCompatibilityHandler
{
    object CompatibleReleaseProfileForSending(SonarrReleaseProfile profile);
    SonarrReleaseProfile CompatibleReleaseProfileForReceiving(JObject profile);
}
