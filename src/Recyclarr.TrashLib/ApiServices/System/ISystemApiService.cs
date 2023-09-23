using Recyclarr.Config.Models;
using Recyclarr.TrashLib.ApiServices.System.Dto;

namespace Recyclarr.TrashLib.ApiServices.System;

public interface ISystemApiService
{
    Task<SystemStatus> GetStatus(IServiceConfiguration config);
}
