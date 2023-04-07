using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.ExceptionTypes;

namespace Recyclarr.TrashLib.Compatibility.Sonarr;

public class SonarrCapabilityEnforcer
{
    private readonly ISonarrCapabilityChecker _capabilityChecker;

    public SonarrCapabilityEnforcer(ISonarrCapabilityChecker capabilityChecker)
    {
        _capabilityChecker = capabilityChecker;
    }

    public async Task Check(SonarrConfiguration config)
    {
        var capabilities = await _capabilityChecker.GetCapabilities(config);
        if (capabilities is null)
        {
            throw new ServiceIncompatibilityException("Capabilities could not be obtained");
        }

        if (!capabilities.SupportsNamedReleaseProfiles)
        {
            throw new ServiceIncompatibilityException(
                $"Your Sonarr version {capabilities.Version} does not meet the minimum " +
                $"required version of {SonarrCapabilities.MinimumVersion}.");
        }

        switch (capabilities.SupportsCustomFormats)
        {
            case true when config.ReleaseProfiles.IsNotEmpty():
                throw new ServiceIncompatibilityException(
                    "Release profiles require Sonarr v3. " +
                    "Please use `custom_formats` instead or use the right version of Sonarr.");

            case false when config.CustomFormats.IsNotEmpty():
                throw new ServiceIncompatibilityException(
                    "Custom formats require Sonarr v4 or greater. " +
                    "Please use `release_profiles` instead or use the right version of Sonarr.");
        }
    }
}
