using Recyclarr.TrashLib.Services.System.Dto;

namespace Recyclarr.TrashLib.Services.System;

public interface ISystemApiService
{
    Task<SystemStatus> GetStatus();
}
