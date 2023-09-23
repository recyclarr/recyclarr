using Recyclarr.Config.Models;

namespace Recyclarr.TrashLib.ApiServices.System;

public interface IServiceInformation
{
    public Task<Version> GetVersion(IServiceConfiguration config);
}
