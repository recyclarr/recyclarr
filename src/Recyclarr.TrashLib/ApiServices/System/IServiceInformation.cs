using Recyclarr.TrashLib.Config;

namespace Recyclarr.TrashLib.ApiServices.System;

public interface IServiceInformation
{
    public Task<Version> GetVersion(IServiceConfiguration config);
}
