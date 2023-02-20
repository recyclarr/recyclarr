using Recyclarr.TrashLib.ApiServices.System;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Compatibility;

public abstract class ServiceCapabilityChecker<T> where T : class
{
    private readonly IServiceInformation _info;

    protected ServiceCapabilityChecker(IServiceInformation info)
    {
        _info = info;
    }

    public async Task<T?> GetCapabilities(IServiceConfiguration config)
    {
        var version = await _info.GetVersion(config);
        return version is not null ? BuildCapabilitiesObject(version) : null;
    }

    protected abstract T BuildCapabilitiesObject(Version version);
}
