using Flurl;

namespace TrashLib.Config.Services;

public interface IServerInfo
{
    Url BuildRequest();
}
