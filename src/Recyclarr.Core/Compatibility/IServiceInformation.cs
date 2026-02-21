namespace Recyclarr.Compatibility;

public interface IServiceInformation
{
    Task<Version> GetVersion(CancellationToken ct);
}
