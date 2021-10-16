using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Flurl;
using Flurl.Http;
using TrashLib.Config;

namespace TrashLib.Sonarr
{
    public class SonarrCompatibility : ISonarrCompatibility
    {
        private Version _version = new();

        public SonarrCompatibility(IServerInfo serverInfo)
        {
            var task = serverInfo.BuildUrl()
                .AppendPathSegment("system/status")
                .GetJsonAsync();

            task.ToObservable()
                .Select(x => new Version(x.version))
                .Subscribe(x => _version = x);
        }

        public bool SupportsNamedReleaseProfiles =>
            _version >= new Version(MinimumVersion);

        // Background: Issue #16 filed which points to a backward-breaking API
        // change made in Sonarr at commit [deed85d2f].
        //
        // [deed85d2f]: https://github.com/Sonarr/Sonarr/commit/deed85d2f9147e6180014507ef4f5af3695b0c61
        public bool ArraysNeededForReleaseProfileRequiredAndIgnored =>
            _version >= new Version("3.0.6.1355");

        public string InformationalVersion => _version.ToString();
        public string MinimumVersion => "3.0.4.1098";
    }
}
