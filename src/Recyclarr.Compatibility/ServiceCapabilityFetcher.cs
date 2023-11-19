using Recyclarr.Config.Models;

namespace Recyclarr.Compatibility;

public abstract class ServiceCapabilityFetcher<T>(IServiceInformation info)
    where T : class
{
    public async Task<T> GetCapabilities(IServiceConfiguration config)
    {
        var version = await info.GetVersion(config);
        return BuildCapabilitiesObject(version);
    }

    protected abstract T BuildCapabilitiesObject(Version version);
}
