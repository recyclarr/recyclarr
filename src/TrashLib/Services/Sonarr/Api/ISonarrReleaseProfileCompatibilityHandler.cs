using Newtonsoft.Json.Linq;
using TrashLib.Services.Sonarr.Api.Objects;

namespace TrashLib.Services.Sonarr.Api;

public interface ISonarrReleaseProfileCompatibilityHandler
{
    Task<object> CompatibleReleaseProfileForSendingAsync(SonarrReleaseProfile profile);
    SonarrReleaseProfile CompatibleReleaseProfileForReceiving(JObject profile);
}
