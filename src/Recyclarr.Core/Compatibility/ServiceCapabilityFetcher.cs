namespace Recyclarr.Compatibility;

public abstract class ServiceCapabilityFetcher<T>(IServiceInformation info)
    where T : class
{
    public async Task<T> GetCapabilities(CancellationToken ct)
    {
        var version = await info.GetVersion(ct);
        return BuildCapabilitiesObject(version);
    }

    protected abstract T BuildCapabilitiesObject(Version version);
}
