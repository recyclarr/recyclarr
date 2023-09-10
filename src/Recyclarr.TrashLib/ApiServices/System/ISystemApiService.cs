using Recyclarr.TrashLib.ApiServices.System.Dto;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.TrashLib.ApiServices.System;

public interface ISystemApiService
{
    Task<SystemStatus> GetStatus(IServiceConfiguration config);
}
