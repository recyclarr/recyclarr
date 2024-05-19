namespace Recyclarr.Compatibility;

public abstract class ServiceCapabilityFetcher<T>(IServiceInformation info)
    where T : class
{
    public async Task<T> GetCapabilities()
    {
        var version = await info.GetVersion();
        return BuildCapabilitiesObject(version);
    }

    protected abstract T BuildCapabilitiesObject(Version version);
}
