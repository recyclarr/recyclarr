using Flurl.Http;
using Recyclarr.Servarr.SystemStatus;

namespace Recyclarr.Compatibility;

public class ServiceInformation(ISystemService api, ILogger log) : IServiceInformation
{
    private Version? _version;

    public async Task<Version> GetVersion(CancellationToken ct)
    {
        return _version ??= await FetchVersion(ct);
    }

    private async Task<Version> FetchVersion(CancellationToken ct)
    {
        try
        {
            var result = await api.GetStatus(ct);
            log.Debug("{Service} Version: {Version}", result.AppName, result.Version);
            return result.Version;
        }
        catch (FlurlHttpException)
        {
            log.Error("Unable to obtain service version information");
            throw;
        }
    }
}
