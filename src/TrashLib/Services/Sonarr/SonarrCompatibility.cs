using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Serilog;
using TrashLib.Services.Sonarr.Api;

namespace TrashLib.Services.Sonarr;

public class SonarrCompatibility : ISonarrCompatibility
{
    private readonly ILogger _log;

    public SonarrCompatibility(ISonarrApi api, ILogger log)
    {
        _log = log;
        Capabilities = Observable.FromAsync(async () => await api.GetVersion(), NewThreadScheduler.Default)
            .Timeout(TimeSpan.FromSeconds(15))
            .Select(BuildCapabilitiesObject)
            .Replay(1)
            .AutoConnect();
    }

    public IObservable<SonarrCapabilities> Capabilities { get; }
    public Version MinimumVersion => new("3.0.4.1098");

    private SonarrCapabilities BuildCapabilitiesObject(Version version)
    {
        _log.Debug("Sonarr Version: {Version}", version);
        return new SonarrCapabilities(version)
        {
            SupportsNamedReleaseProfiles =
                version >= MinimumVersion,

            ArraysNeededForReleaseProfileRequiredAndIgnored =
                version >= new Version("3.0.6.1355"),

            SupportsCustomFormats =
                version >= new Version(4, 0)
        };
    }
}
