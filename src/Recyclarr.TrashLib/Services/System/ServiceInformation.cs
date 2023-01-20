using Flurl.Http;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;

namespace Recyclarr.TrashLib.Services.System;

public class ServiceInformation : IServiceInformation
{
    private readonly ISystemApiService _api;
    private readonly ILogger _log;

    public ServiceInformation(ISystemApiService api, ILogger log)
    {
        _api = api;
        _log = log;
    }

    public async Task<Version?> GetVersion(IServiceConfiguration config)
    {
        try
        {
            var status = await _api.GetStatus(config);
            _log.Debug("{Service} Version: {Version}", status.AppName, status.Version);
            return new Version(status.Version);
        }
        catch (FlurlHttpException ex)
        {
            _log.Error("Exception trying to obtain service version: {Message}", ex.SanitizedExceptionMessage());
        }

        return null;
    }
}
