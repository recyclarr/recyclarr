using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Recyclarr.TrashLib.Services.System.Dto;
using Serilog;

namespace Recyclarr.TrashLib.Services.System;

public abstract class ServiceCompatibility<T> where T : new()
{
    private readonly ILogger _log;

    protected ServiceCompatibility(ISystemApiService api, ILogger log)
    {
        _log = log;
        Capabilities = Observable.FromAsync(async () => await api.GetStatus(), NewThreadScheduler.Default)
            .Timeout(TimeSpan.FromSeconds(15))
            .Do(LogServiceInfo)
            .Select(x => new Version(x.Version))
            .Select(BuildCapabilitiesObject)
            .Replay(1)
            .AutoConnect();
    }

    public IObservable<T> Capabilities { get; }

    private void LogServiceInfo(SystemStatus status)
    {
        _log.Debug("{Service} Version: {Version}", status.AppName, status.Version);
    }

    protected abstract T BuildCapabilitiesObject(Version version);
}
