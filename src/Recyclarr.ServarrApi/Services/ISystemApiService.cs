using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Dto;

namespace Recyclarr.ServarrApi.Services;

public interface ISystemApiService
{
    Task<SystemStatus> GetStatus(IServiceConfiguration config);
}
