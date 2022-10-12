using System.Reactive.Linq;
using TrashLib.ExceptionTypes;
using TrashLib.Services.Sonarr;
using TrashLib.Services.Sonarr.Config;

namespace Recyclarr.Command;

public class SonarrVersionEnforcement : ISonarrVersionEnforcement
{
    private readonly ISonarrCompatibility _compatibility;

    public SonarrVersionEnforcement(ISonarrCompatibility compatibility)
    {
        _compatibility = compatibility;
    }

    public async Task DoVersionEnforcement(SonarrConfiguration config)
    {
        var capabilities = await _compatibility.Capabilities.LastAsync();
        if (!capabilities.SupportsNamedReleaseProfiles)
        {
            throw new VersionException(
                $"Your Sonarr version {capabilities.Version} does not meet the minimum " +
                $"required version of {_compatibility.MinimumVersion} to use this program");
        }

        switch (capabilities.SupportsCustomFormats)
        {
            case true when config.ReleaseProfiles.Any():
                throw new VersionException(
                    "Sonarr v4 does not support Release Profiles. Please use Sonarr v3 instead.");

            case false when config.CustomFormats.Any():
                throw new VersionException(
                    "Sonarr v3 does not support Custom Formats. Please use Sonarr v4 instead.");
        }
    }
}
