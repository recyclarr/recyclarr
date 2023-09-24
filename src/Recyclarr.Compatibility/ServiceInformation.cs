using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Services;
using Serilog;

namespace Recyclarr.Compatibility;

public class ServiceInformation : IServiceInformation
{
    private readonly ISystemApiService _api;
    private readonly ILogger _log;

    public ServiceInformation(ISystemApiService api, ILogger log)
    {
        _api = api;
        _log = log;
    }

    public async Task<Version> GetVersion(IServiceConfiguration config)
    {
        try
        {
            var status = await _api.GetStatus(config);
            _log.Debug("{Service} Version: {Version}", status.AppName, status.Version);
            return new Version(status.Version);
        }
        catch (FlurlHttpException)
        {
            _log.Error("Unable to obtain service version information");
            throw;
        }
    }
}
