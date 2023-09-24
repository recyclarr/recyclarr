using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;

namespace Recyclarr.Compatibility.Sonarr;

public class SonarrCapabilityEnforcer
{
    private readonly ISonarrCapabilityFetcher _capabilityFetcher;

    public SonarrCapabilityEnforcer(ISonarrCapabilityFetcher capabilityFetcher)
    {
        _capabilityFetcher = capabilityFetcher;
    }

    public async Task Check(SonarrConfiguration config)
    {
        var capabilities = await _capabilityFetcher.GetCapabilities(config);

        if (capabilities.Version < SonarrCapabilities.MinimumVersion)
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

        // Check for aspects of quality profile sync that are not supported by Sonarr v3
        if (!capabilities.SupportsCustomFormats)
        {
            if (config.QualityProfiles.Any(x => x.UpgradeUntilScore is not null))
            {
                throw new ServiceIncompatibilityException(
                    "`until_score` under `upgrade` is not supported by Sonarr v3. " +
                    "Remove the until_score property or use Sonarr v4.");
            }

            if (config.QualityProfiles.Any(x => x.MinFormatScore is not null))
            {
                throw new ServiceIncompatibilityException(
                    "`min_format_score` under `quality_profiles` is not supported by Sonarr v3. " +
                    "Remove the min_format_score property or use Sonarr v4.");
            }
        }
    }
}
