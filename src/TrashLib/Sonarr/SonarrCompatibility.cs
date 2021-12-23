using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Flurl.Http;
using TrashLib.Config.Services;

namespace TrashLib.Sonarr;

public class SonarrCompatibility : ISonarrCompatibility
{
    public SonarrCompatibility(IServerInfo serverInfo)
    {
        Capabilities = Observable.FromAsync(
                async () => await serverInfo.BuildRequest()
                    .AppendPathSegment("system/status")
                    .GetJsonAsync(), NewThreadScheduler.Default)
            .Timeout(TimeSpan.FromSeconds(15))
            .Select(x => new Version(x.version))
            .Select(BuildCapabilitiesObject)
            .Replay(1)
            .AutoConnect();
    }

    public IObservable<SonarrCapabilities> Capabilities { get; }
    public Version MinimumVersion => new("3.0.4.1098");

    private SonarrCapabilities BuildCapabilitiesObject(Version version)
    {
        return new SonarrCapabilities(version)
        {
            SupportsNamedReleaseProfiles =
                version >= MinimumVersion,

            ArraysNeededForReleaseProfileRequiredAndIgnored =
                version >= new Version("3.0.6.1355")
        };
    }
}
