using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.System;

public interface ISystemApiService
{
    Task<SystemStatus> GetStatus(IServiceConfiguration config);
}
