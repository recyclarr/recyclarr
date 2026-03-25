namespace Recyclarr.Compatibility;

public interface IServiceInformation
{
    Task<Version> GetVersion(CancellationToken ct);
    Task<string> GetAppName(CancellationToken ct);
}
