using Newtonsoft.Json.Linq;
using TrashLib.Sonarr.Api.Objects;

namespace TrashLib.Sonarr.Api
{
    public interface ISonarrReleaseProfileCompatibilityHandler
    {
        object CompatibleReleaseProfileForSending(SonarrReleaseProfile profile);
        SonarrReleaseProfile CompatibleReleaseProfileForReceiving(JObject profile);
    }
}
