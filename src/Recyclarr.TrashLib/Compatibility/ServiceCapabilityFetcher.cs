using Recyclarr.Config.Models;

namespace Recyclarr.TrashLib.Compatibility;

public abstract class ServiceCapabilityFetcher<T> where T : class
{
    private readonly IServiceInformation _info;

    protected ServiceCapabilityFetcher(IServiceInformation info)
    {
        _info = info;
    }

    public async Task<T> GetCapabilities(IServiceConfiguration config)
    {
        var version = await _info.GetVersion(config);
        return BuildCapabilitiesObject(version);
    }

    protected abstract T BuildCapabilitiesObject(Version version);
}
