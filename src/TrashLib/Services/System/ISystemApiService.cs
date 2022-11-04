using TrashLib.Services.System.Dto;

namespace TrashLib.Services.System;

public interface ISystemApiService
{
    Task<SystemStatus> GetStatus();
}
