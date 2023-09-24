using Recyclarr.Config.Models;

namespace Recyclarr.Compatibility;

public interface IServiceInformation
{
    public Task<Version> GetVersion(IServiceConfiguration config);
}
