using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.System;

public interface IServiceInformation
{
    public Task<Version?> GetVersion(IServiceConfiguration config);
}
