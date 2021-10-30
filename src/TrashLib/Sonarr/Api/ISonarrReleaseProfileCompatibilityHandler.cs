using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TrashLib.Sonarr.Api.Objects;

namespace TrashLib.Sonarr.Api
{
    public interface ISonarrReleaseProfileCompatibilityHandler
    {
        Task<object> CompatibleReleaseProfileForSendingAsync(SonarrReleaseProfile profile);
        SonarrReleaseProfile CompatibleReleaseProfileForReceiving(JObject profile);
    }
}
