using System.Reactive.Linq;
using Recyclarr.TrashLib.Services.System;

namespace Recyclarr.TrashLib.Services.Common;

public abstract class ServiceCapabilityChecker<T> where T : class
{
    private readonly IObservable<T?> _capabilities;

    public T? GetCapabilities() => _capabilities.Wait();

    protected ServiceCapabilityChecker(IServiceInformation info)
    {
        _capabilities = info.Version
            .Select(x => x is null ? null : BuildCapabilitiesObject(x))
            .Replay(1)
            .AutoConnect()
            .LastAsync();
    }

    protected abstract T BuildCapabilitiesObject(Version version);
}
