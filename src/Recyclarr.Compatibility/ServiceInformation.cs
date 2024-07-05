using Flurl.Http;
using Recyclarr.ServarrApi.System;
using Serilog;

namespace Recyclarr.Compatibility;

public class ServiceInformation(ISystemApiService api, ILogger log) : IServiceInformation
{
    public async Task<Version> GetVersion(CancellationToken ct)
    {
        try
        {
            var status = await api.GetStatus(ct);
            log.Debug("{Service} Version: {Version}", status.AppName, status.Version);
            return new Version(status.Version);
        }
        catch (FlurlHttpException)
        {
            log.Error("Unable to obtain service version information");
            throw;
        }
    }
}
