using Recyclarr.Servarr.SystemStatus;

namespace Recyclarr.Compatibility;

public class ServiceInformation(ISystemService api, ILogger log) : IServiceInformation
{
    private SystemServiceResult? _status;

    public async Task<Version> GetVersion(CancellationToken ct)
    {
        return (await FetchStatus(ct)).Version;
    }

    public async Task<string> GetAppName(CancellationToken ct)
    {
        return (await FetchStatus(ct)).AppName;
    }

    private async Task<SystemServiceResult> FetchStatus(CancellationToken ct)
    {
        return _status ??= await DoFetchStatus(ct);
    }

    private async Task<SystemServiceResult> DoFetchStatus(CancellationToken ct)
    {
        try
        {
            var result = await api.GetStatus(ct);
            log.Debug("{Service} Version: {Version}", result.AppName, result.Version);
            return result;
        }
        catch (HttpRequestException)
        {
            log.Error("Unable to obtain service version information");
            throw;
        }
    }
}
