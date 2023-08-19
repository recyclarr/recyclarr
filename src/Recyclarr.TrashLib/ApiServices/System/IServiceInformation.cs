using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.ApiServices.System;

public interface IServiceInformation
{
    public Task<Version> GetVersion(IServiceConfiguration config);
}
