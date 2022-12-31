using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Services.Sonarr.Api.Objects;

namespace Recyclarr.TrashLib.Services.Sonarr.Api;

public interface ISonarrReleaseProfileCompatibilityHandler
{
    object CompatibleReleaseProfileForSending(SonarrReleaseProfile profile);
    SonarrReleaseProfile CompatibleReleaseProfileForReceiving(JObject profile);
}
