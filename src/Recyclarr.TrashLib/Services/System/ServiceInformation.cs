using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Flurl.Http;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Services.System.Dto;

namespace Recyclarr.TrashLib.Services.System;

public class ServiceInformation : IServiceInformation
{
    private readonly ILogger _log;

    public ServiceInformation(ISystemApiService api, ILogger log)
    {
        _log = log;
        Version = Observable.FromAsync(async () => await api.GetStatus(), ThreadPoolScheduler.Instance)
            .Timeout(TimeSpan.FromSeconds(15))
            .Do(LogServiceInfo)
            .Select(x => new Version(x.Version))
            .Catch((Exception ex) =>
            {
                log.Error("Exception trying to obtain service version: {Message}", ex switch
                {
                    FlurlHttpException flex => flex.SanitizedExceptionMessage(),
                    _ => ex.Message
                });

                return Observable.Return((Version?) null);
            })
            .Replay(1)
            .AutoConnect();
    }

    public IObservable<Version?> Version { get; }

    private void LogServiceInfo(SystemStatus status)
    {
        _log.Debug("{Service} Version: {Version}", status.AppName, status.Version);
    }
}
