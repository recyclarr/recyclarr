using System.Reactive.Linq;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Services.System;

namespace Recyclarr.TrashLib.Services.Common;

public abstract class ServiceCompatibility<T> where T : class
{
    private readonly IObservable<T> _capabilities;

    public T Capabilities => _capabilities.Wait();

    protected ServiceCompatibility(IServiceInformation compatibility)
    {
        _capabilities = compatibility.Version
            .Select(BuildCapabilitiesObject)
            .Replay(1)
            .AutoConnect()
            .NotNull()
            .LastAsync();
    }

    protected abstract T BuildCapabilitiesObject(Version version);
}
