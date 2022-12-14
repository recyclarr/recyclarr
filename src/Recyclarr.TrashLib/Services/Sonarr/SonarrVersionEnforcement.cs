using System.Reactive.Linq;
using Recyclarr.TrashLib.ExceptionTypes;
using Recyclarr.TrashLib.Services.Sonarr.Config;

namespace Recyclarr.TrashLib.Services.Sonarr;

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
                    "Sonarr v4 does not support Release Profiles. Please use Custom Formats instead.");

            case false when config.CustomFormats.Any():
                throw new VersionException(
                    "Sonarr v3 does not support Custom Formats. Please use Release Profiles instead.");
        }
    }
}
