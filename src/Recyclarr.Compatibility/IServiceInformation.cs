namespace Recyclarr.Compatibility;

public interface IServiceInformation
{
    public Task<Version> GetVersion(CancellationToken ct);
}
