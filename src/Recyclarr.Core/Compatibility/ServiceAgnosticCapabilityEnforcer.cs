using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;

namespace Recyclarr.Compatibility;

public class ServiceAgnosticCapabilityEnforcer(
    IServiceInformation serviceInfo,
    SonarrCapabilityEnforcer sonarrEnforcer,
    RadarrCapabilityEnforcer radarrEnforcer
)
{
    public async Task Check(IServiceConfiguration config, CancellationToken ct)
    {
        await CheckServiceTypeMismatch(config, ct);

        switch (config)
        {
            case SonarrConfiguration:
                await sonarrEnforcer.Check(ct);
                break;

            case RadarrConfiguration:
                await radarrEnforcer.Check(ct);
                break;
        }
    }

    private async Task CheckServiceTypeMismatch(IServiceConfiguration config, CancellationToken ct)
    {
        var appName = await serviceInfo.GetAppName(ct);
        var expectedService = config.ServiceType.ToString();

        if (!appName.Equals(expectedService, StringComparison.OrdinalIgnoreCase))
        {
            throw new ServiceIncompatibilityException(
                $"Configuration is for {expectedService}, but the service at "
                    + $"'{config.BaseUrl}' is {appName}. Check your 'base_url' configuration."
            );
        }
    }
}
