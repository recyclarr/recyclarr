using Recyclarr.Config.Models;

namespace Recyclarr.TrashLib.Compatibility;

public interface IServiceInformation
{
    public Task<Version> GetVersion(IServiceConfiguration config);
}
