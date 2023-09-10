using Newtonsoft.Json.Linq;
using Recyclarr.Cli.Pipelines.ReleaseProfile.Api.Objects;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Api;

public interface ISonarrReleaseProfileCompatibilityHandler
{
    Task<object> CompatibleReleaseProfileForSending(
        IServiceConfiguration config,
        SonarrReleaseProfile profile);

    SonarrReleaseProfile CompatibleReleaseProfileForReceiving(JObject profile);
}
